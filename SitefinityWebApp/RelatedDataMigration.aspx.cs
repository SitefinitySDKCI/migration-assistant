using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.Data.ContentLinks;
using Telerik.Sitefinity.Data.DataSource;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.DynamicModules.Builder;
using Telerik.Sitefinity.DynamicModules.Builder.Model;
using Telerik.Sitefinity.DynamicModules.Model;
using Telerik.Sitefinity.GenericContent.Model;
using Telerik.Sitefinity.Lifecycle;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.Model.ContentLinks;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.RelatedData;
using Telerik.Sitefinity.Security.Claims;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Utilities.TypeConverters;

namespace SitefinityWebApp
{
    public partial class RelatedDataMigration : System.Web.UI.Page
    {
        #region Public Properties

        public ModuleBuilderManager ModuleBuilderMngr
        {
            get
            {
                return this.moduleBuilderMngr.Value;
            }
        }

        public DynamicModuleManager DynamicModulerMngr
        {
            get
            {
                return this.dynamicModulerMngr.Value;
            }
        }

        public IEnumerable<DynamicModuleType> DynamicModuleTypes
        {
            get
            {
                return this.ModuleBuilderMngr.GetItems(typeof(DynamicModuleType), string.Empty, string.Empty, 0, 0).OfType<DynamicModuleType>();
            }
        }

        public string ParentItemTypeName
        {
            get
            {
                return this.parentItemTypeName;
            }
            set
            {
                this.parentItemTypeName = value;
            }
        }

        public string ParentItemProviderName
        {
            get
            {
                return this.parentItemProviderName;
            }
            set
            {
                this.parentItemProviderName = value;
            }
        }

        public string ChildItemTypeName
        {
            get
            {
                return this.childItemTypeName;
            }
            set
            {
                this.childItemTypeName = value;
            }
        }

        public string ChildItemProviderName
        {
            get
            {
                return this.childItemProviderName;
            }
            set
            {
                this.childItemProviderName = value;
            }
        }

        public string DynamicSelectorFieldName
        {
            get
            {
                return this.dynamicSelectorFieldName;
            }
            set
            {
                this.dynamicSelectorFieldName = value;
            }
        }

        public string RelatedDataFieldName
        {
            get
            {
                return this.relatedDataFieldName;
            }
            set
            {
                this.relatedDataFieldName = value;
            }
        }

        public Type ParentItemType
        {
            get
            {
                return this.parentItemType;
            }
            set
            {
                this.parentItemType = value;
            }
        }

        public Type ChildItemType
        {
            get
            {
                return this.childItemType;
            }
            set
            {
                this.childItemType = value;
            }
        }

        #endregion

        #region Page Members

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            this.ddlParentItemTypeName.SelectedIndexChanged += DdlParentItemTypeName_SelectedIndexChanged;
            this.ddlParentItemTypeName.DataBound += DdlParentItemTypeName_SelectedIndexChanged;
            this.ddlChildItemTypeName.SelectedIndexChanged += DdlChildItemTypeName_SelectedIndexChanged;
            this.ddlChildItemTypeName.DataBound += DdlChildItemTypeName_SelectedIndexChanged;
            this.migrateDataBtn.Click += MigrateDataBtnClick;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var user = ClaimsManager.GetCurrentIdentity();
            if (!user.IsAuthenticated)
            {
                this.loginStatusLabel.Text = "Please authenticate in order to make the migration.";
                return;
            }
            this.loginStatusLabel.Text = String.Format("You are logged in as: {0}. Good luck!", user.Name);
            this.migrateDataBtn.Enabled = this.ddlMigrationType.SelectedValue != "-1";

            if (!this.IsPostBack || Request["__EVENTTARGET"] == this.ddlMigrationType.ID)
            {
                this.BindDropDowns();
            }
        }

        void DdlChildItemTypeName_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ProviderModel> childTypeProviders = null;

            if (this.ddlMigrationType.SelectedValue == "Media")
            {
                childTypeProviders = this.SetLibraryProviders("Telerik.Sitefinity.Modules.Libraries.LibrariesManager");
            }
            else if (this.ddlMigrationType.SelectedValue != "-1")
            {
                var currentParentType = this.DynamicModuleTypes.FirstOrDefault(dmt => dmt.GetFullTypeName() == ((DropDownList)sender).SelectedValue);
                if (currentParentType != null)
                {
                    childTypeProviders = this.SetProviders(currentParentType.ModuleName);
                }
            }

            this.ddlChildItemProviderName.DataSource = childTypeProviders;
            this.ddlChildItemProviderName.DataBind();
        }

        void DdlParentItemTypeName_SelectedIndexChanged(object sender, EventArgs e)
        {
            var currentParentType = this.DynamicModuleTypes.FirstOrDefault(dmt => dmt.GetFullTypeName() == ((DropDownList)sender).SelectedValue);
            if (currentParentType != null)
            {
                var parentTypeProviders = this.SetProviders(currentParentType.ModuleName);

                this.ddlParentItemProviderName.DataSource = parentTypeProviders;
                this.ddlParentItemProviderName.DataBind();
            }
        }

        void MigrateDataBtnClick(object sender, EventArgs e)
        {
            this.migrateDataBtn.Enabled = false;
            StartTimer();

            SystemManager.RunWithElevatedPrivilegeDelegate doWork = new SystemManager.RunWithElevatedPrivilegeDelegate(this.Migrate);
            SystemManager.BackgroundTasksService.EnqueueTask(() => SystemManager.RunWithElevatedPrivilege(doWork));
        }

        private void Migrate(object[] parameters)
        {
            string transactionName = Guid.NewGuid().ToString();
            try
            {
                this.MigrateData(transactionName);
            }
            catch (Exception ex)
            {
                this.Log(String.Format(logError, ex.Message), true);
                this.Log(transactionRollbackMessage);
                TransactionManager.RollbackTransaction(transactionName);
                this.Log(migrationErrorMessage);
            }
            RelatedDataMigration.runTimer = false;
        }

        #endregion

        #region Private Methods

        private void BindDropDowns()
        {
            List<DropDownItem> parentItemTypesDataSource = this.DynamicModuleTypes.Select(Map).ToList();
            List<DropDownItem> childItemTypesDataSource = null;

            //Parent Type Items
            this.ddlParentItemTypeName.DataSource = parentItemTypesDataSource;
            this.ddlParentItemTypeName.DataBind();

            //Child Type Items
            if (this.ddlMigrationType.SelectedValue == "Media")
            {
                childItemTypesDataSource = this.librariesDataSource;
            }
            else if (this.ddlMigrationType.SelectedValue != "-1")
            {
                childItemTypesDataSource = parentItemTypesDataSource;
            }

            this.ddlChildItemTypeName.DataSource = childItemTypesDataSource;
            this.ddlChildItemTypeName.DataBind();
        }

        private void MigrateData(string transactionName)
        {
            this.SetMigartionProperies();

            var migrationType = this.ddlMigrationType.SelectedValue;
            switch (migrationType)
            {
                case "GuidArray":
                    this.MigrateGuidArray(transactionName);
                    break;
                case "Guid":
                    this.MigrateGuid(transactionName);
                    break;
                case "Media":
                    this.MigrateMedia(transactionName);
                    break;
            }

            this.Log(transactionCommitMessage);
            TransactionManager.CommitTransaction(transactionName);
            this.Log(migrationCompleteMessage);
        }

        private void MigrateMedia(string transactionName)
        {
            DynamicModuleManager dynamicModuleManager = DynamicModuleManager.GetManager(parentItemProviderName, transactionName);

            var parentItems = dynamicModuleManager.GetDataItems(parentItemType);
            var allItemsCount = parentItems.Count();
            var currentIndex = 1;
            var sliceIterator = parentItems.SliceQuery(1000);

            // this manager will serve only for checking if child items exist, it should work with childItemProvider
            var childItemsManager = ManagerBase.GetMappedManagerInTransaction(childItemType, childItemProviderName, transactionName);

            var managerType = ManagerBase.GetMappedManagerType(typeof(ContentLink));
            var contentLinksManager = ManagerBase.GetManagerInTransaction(managerType, null, transactionName) as ContentLinksManager;
            var appName = contentLinksManager.Provider.ApplicationName;

            var librariesManager = LibrariesManager.GetManager(childItemProviderName);
            using (new ElevatedModeRegion(librariesManager))
            {
                foreach (var slicedQuery in sliceIterator)
                {
                    var currentQuery = slicedQuery;
                    foreach (var parentItem in currentQuery)
                    {
                        // get selected items for current parent item
                        var mediaContentLinks = parentItem.GetValue<ContentLink[]>(dynamicSelectorFieldName).ToArray();
                        foreach (var contentLink in mediaContentLinks)
                        {
                            ILifecycleDataItem currentItem = librariesManager.GetItemOrDefault(childItemType, contentLink.ChildItemId) as ILifecycleDataItem;
                            if (currentItem != null)
                            {
                                var masterContentItem = librariesManager.Lifecycle.GetMaster(currentItem);

                                this.AddRelationToItem(childItemsManager, contentLinksManager, appName, parentItem, masterContentItem.Id, contentLink.Ordinal, currentIndex, allItemsCount);
                            }
                        }
                        currentIndex++;
                    }

                    TransactionManager.FlushTransaction(transactionName);
                }
            }
            currentIndex = 1;
        }

        private void MigrateGuid(string transactionName)
        {
            DynamicModuleManager dynamicModuleManager = DynamicModuleManager.GetManager(parentItemProviderName, transactionName);

            var parentItems = dynamicModuleManager.GetDataItems(parentItemType);
            var allItemsCount = parentItems.Count();
            var currentIndex = 1;
            var sliceIterator = parentItems.SliceQuery(1000);

            // this manager will serve only for checking if child items exist, it should work with childItemProvider
            var childItemsManager = ManagerBase.GetMappedManagerInTransaction(childItemType, childItemProviderName, transactionName);

            var managerType = ManagerBase.GetMappedManagerType(typeof(ContentLink));
            var contentLinksManager = ManagerBase.GetManagerInTransaction(managerType, null, transactionName) as ContentLinksManager;
            var appName = contentLinksManager.Provider.ApplicationName;

            Guid childItemId;
            foreach (var slicedQuery in sliceIterator)
            {
                var currentQuery = slicedQuery;
                foreach (var parentItem in currentQuery)
                {
                    // get selected items for current parent item
                    childItemId = parentItem.GetValue<Guid>(dynamicSelectorFieldName);
                    if (childItemId != Guid.Empty)
                    {
                        this.AddRelationToItem(childItemsManager, contentLinksManager, appName, parentItem, childItemId, 0, currentIndex, allItemsCount);
                    }
                    currentIndex++;
                }

                TransactionManager.FlushTransaction(transactionName);
            }
            currentIndex = 1;
        }

        private void MigrateGuidArray(string transactionName)
        {
            DynamicModuleManager dynamicModuleManager = DynamicModuleManager.GetManager(parentItemProviderName, transactionName);

            var parentItems = dynamicModuleManager.GetDataItems(parentItemType);
            var allItemsCount = parentItems.Count();
            var currentIndex = 1;
            var sliceIterator = parentItems.SliceQuery(1000);

            // this manager will serve only for checking if child items exist, it should work with childItemProvider
            var childItemsManager = ManagerBase.GetMappedManagerInTransaction(childItemType, childItemProviderName, transactionName);

            var managerType = ManagerBase.GetMappedManagerType(typeof(ContentLink));
            var contentLinksManager = ManagerBase.GetManagerInTransaction(managerType, null, transactionName) as ContentLinksManager;
            var appName = contentLinksManager.Provider.ApplicationName;

            Guid[] childItemIds;
            float childItemOrdinal;
            foreach (var slicedQuery in sliceIterator)
            {
                var currentQuery = slicedQuery;
                foreach (var parentItem in currentQuery)
                {
                    childItemOrdinal = 0;
                    // get selected items for current parent item
                    childItemIds = parentItem.GetValue<Guid[]>(dynamicSelectorFieldName);
                    if (childItemIds != null)
                    {
                        foreach (var id in childItemIds)
                        {
                            this.AddRelationToItem(childItemsManager, contentLinksManager, appName, parentItem, id, childItemOrdinal, currentIndex, allItemsCount);
                            childItemOrdinal++;
                        }
                    }
                    currentIndex++;
                }

                TransactionManager.FlushTransaction(transactionName);
            }
            currentIndex = 1;
        }

        private void SetMigartionProperies()
        {
            this.ParentItemTypeName = this.ddlParentItemTypeName.SelectedValue;
            this.ParentItemProviderName = this.ddlParentItemProviderName.SelectedValue;
            this.ChildItemTypeName = this.ddlChildItemTypeName.SelectedValue;
            this.ChildItemProviderName = this.ddlChildItemProviderName.SelectedValue;
            this.DynamicSelectorFieldName = this.dynamicSelectorFieldNameTextBox.Text;
            this.RelatedDataFieldName = this.relatedDataFieldNameTextBox.Text;
            this.ParentItemType = TypeResolutionService.ResolveType(parentItemTypeName);
            this.ChildItemType = TypeResolutionService.ResolveType(childItemTypeName);
        }

        private void AddRelationToItem(IManager childItemsDynamicModuleManager, ContentLinksManager contentLinksManager, string appName, DynamicContent parentItem, Guid childItemId, float ordinal, int currentItemIndex = 0, int allCount = 0)
        {
            using (new ElevatedModeRegion(childItemsDynamicModuleManager))
            {
                var childItem = childItemsDynamicModuleManager.GetItemOrDefault(this.ChildItemType, childItemId) as IDataItem;
                if (childItem != null)
                {
                    var relation = parentItem.CreateRelation(childItem, this.RelatedDataFieldName);
                    this.Log(String.Format(logRelationCreated, parentItem.Id, parentItem.Status, childItemId, (parentItem as IHasTitle).GetTitle(), currentItemIndex, allCount));

                    //Related Data API will be modified to return the created content link or will receive ordinal as parameter. But for now (7.0.5100), we have to get the content link this way.
                    if (relation != null)
                    {
                        relation.Ordinal = ordinal;
                    }
                }
                else
                {
                    // log that item with that id from that provider and type was not found
                    this.Log(String.Format(logRelatedItemDoesntExist, childItemId));
                    this.Log(String.Format(logRelatedItemDoesntExist, childItemId), true);
                }
            }

            this.Log(String.Format(logRelatedItemExist, parentItem.Id, currentItemIndex, allCount));
        }

        private ContentLink GetRelation(ContentLinksManager contentLinksManager, string appName, DynamicContent parentItem, Guid childItemId)
        {
            var relationId = parentItem.Status == ContentLifecycleStatus.Master ? parentItem.Id : parentItem.OriginalContentId;

            var relationFunc = new Func<ContentLink, bool>((c) => c.ParentItemId == relationId &&
                                                                   c.ParentItemType == this.ParentItemTypeName &&
                                                                   c.ParentItemProviderName == this.ParentItemProviderName &&
                                                                   c.ComponentPropertyName == this.RelatedDataFieldName &&
                                                                   c.ChildItemId == childItemId &&
                                                                   c.ChildItemProviderName == this.ChildItemProviderName &&
                                                                   c.ChildItemType == this.ChildItemTypeName);

            var masterRelation = contentLinksManager.GetContentLinks().Where(c => c.ApplicationName == appName)
                                                       .Where(relationFunc).FirstOrDefault();
            // check for relation in dirty items
            if (masterRelation == null)
            {
                masterRelation = contentLinksManager.Provider.GetDirtyItems().OfType<ContentLink>().Where(c => c.ApplicationName == appName)
                                                           .Where(relationFunc).FirstOrDefault();
            }

            return masterRelation;
        }

        private List<ProviderModel> SetProviders(string dataSourceName)
        {
            IEnumerable<DataProviderInfo> providersInfo = new List<DataProviderInfo>().AsQueryable();
            if (SystemManager.CurrentContext.IsMultisiteMode)
            {
                var site = SystemManager.CurrentContext.CurrentSite;
                var providersNames = site.GetProviders(dataSourceName).Select(dsl => dsl.ProviderName);
                providersInfo = this.DynamicModulerMngr.ProviderInfos.Where(pi => providersNames.Contains(pi.ProviderName));
            }
            else
            {
                providersInfo = this.DynamicModulerMngr.ProviderInfos;
            }

            List<ProviderModel> providers = providersInfo.Select(Map).ToList();

            return providers;
        }

        private List<ProviderModel> SetLibraryProviders(string dataSourceName)
        {
            var librariesManager = LibrariesManager.GetManager();
            IEnumerable<DataProviderInfo> providersInfo = new List<DataProviderInfo>().AsQueryable();
            if (SystemManager.CurrentContext.IsMultisiteMode)
            {
                var site = SystemManager.CurrentContext.CurrentSite;
                var providersNames = site.GetProviders(dataSourceName).Select(dsl => dsl.ProviderName);
                providersInfo = librariesManager.ProviderInfos.Where(pi => providersNames.Contains(pi.ProviderName));
            }
            else
            {
                providersInfo = librariesManager.ProviderInfos;
            }

            List<ProviderModel> providers = providersInfo.Select(Map).ToList();

            return providers;
        }

        private DropDownItem Map(DynamicModuleType dynamicModuleType)
        {
            return new DropDownItem
            {
                Value = dynamicModuleType.GetFullTypeName(),
                Text = String.Format("{0} ({1})", dynamicModuleType.TypeName, dynamicModuleType.ModuleName)
            };
        }

        private ProviderModel Map(DataProviderInfo providerInfo)
        {
            return new ProviderModel()
            {
                Name = providerInfo.ProviderName,
                Title = providerInfo.ProviderTitle
            };
        }

        #endregion

        #region Private Fields

        private Lazy<ModuleBuilderManager> moduleBuilderMngr = new Lazy<ModuleBuilderManager>(() => ModuleBuilderManager.GetManager());
        private Lazy<DynamicModuleManager> dynamicModulerMngr = new Lazy<DynamicModuleManager>(() => DynamicModuleManager.GetManager());
        private string parentItemTypeName = string.Empty;
        private string parentItemProviderName = string.Empty;
        private string childItemTypeName = string.Empty;
        private string childItemProviderName = string.Empty;
        private string dynamicSelectorFieldName = string.Empty;
        private string relatedDataFieldName = string.Empty;
        private Type parentItemType;
        private Type childItemType;
        private List<DropDownItem> librariesDataSource = new List<DropDownItem>() 
        { 
            new DropDownItem() { Text = "Document", Value = "Telerik.Sitefinity.Libraries.Model.Document" },
            new DropDownItem() { Text = "Image", Value = "Telerik.Sitefinity.Libraries.Model.Image" },
            new DropDownItem() { Text = "Video", Value = "Telerik.Sitefinity.Libraries.Model.Video" } 
        };
        private static string transactionCommitMessage = "Committing transaction ...";
        private static string transactionRollbackMessage = "Rolling back transaction ...";
        private static string migrationCompleteMessage = "Data migration completed successfully!";
        private static string migrationErrorMessage = "Data migration failed!";
        private static string logError = "The following error occurred: {0}";
        private static string logRelationCreated = "Created relation from: Title {3}, ID {0} status {1} to: ID {2} ... {4}/{5}";
        private static string logRelatedItemDoesntExist = "Related item: ID {0} doesn't exist. Skipping ... ";
        private static string logRelatedItemExist = "Related item: ID {0} exist. Updating ... {1}/{2}";

        #endregion

        #region Migration Log

        private void Log(string message, bool isError = false)
        {
            RelatedDataMigration.logMessage = message;
            if (isError)
            {
                Telerik.Sitefinity.Abstractions.Log.Write(message, ConfigurationPolicy.ErrorLog);
            }
            else
            {
                Telerik.Sitefinity.Abstractions.Log.Write(message, ConfigurationPolicy.Migration);
            }
        }

        private void StartTimer()
        {
            RelatedDataMigration.logMessage = String.Empty;
            this.TimerStatusUpdate.Enabled = runTimer = true;
        }

        protected void TimerStatusUpdate_Tick(object sender, EventArgs e)
        {
            if (!runTimer)
                this.TimerStatusUpdate.Enabled = false;

            this.MigrationLog.Text = RelatedDataMigration.logMessage;
        }

        private static string logMessage = "";
        private static bool runTimer = false;

        #endregion
    }

    /// <summary>
    /// DropDown item model
    /// </summary>
    public class DropDownItem
    {
        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the text
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// Data source provider model
    /// </summary>
    public class ProviderModel
    {
        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        /// <value>
        /// The provider name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the provider title.
        /// </summary>
        /// <value>
        /// The provider title.
        /// </value>
        public string Title { get; set; }
    }

    /// <summary>
    /// Provides extension methods used from the migration script
    /// </summary>
    public static class MigrationExtensions
    {
        public static IEnumerable<IQueryable<DynamicContent>> SliceQuery(this IQueryable<DynamicContent> query, int sliceSize)
        {
            int j = 0;
            int itemsCount = query.Count();

            if (sliceSize >= itemsCount || sliceSize <= 0)
            {
                yield return query;

            }
            else
                while (j < itemsCount)
                {

                    var sliceQuery = query.Skip(j).Take(sliceSize);
                    yield return sliceQuery;
                    j += sliceSize;
                }
        }
    }
}