<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RelatedDataMigration.aspx.cs" Inherits="SitefinityWebApp.RelatedDataMigration" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Related Data Migration Assistant</title>
    <style>
        body, div, dl, dt, dd, ul, ol, li, h1, h2, h3, h4, h5, h6, pre, form, fieldset, input, textarea, p, blockquote, th, td {
            margin: 0;
            padding: 0;
        }

        table {
            border-collapse: collapse;
            border-spacing: 0;
        }

        fieldset, img {
            border: 0;
        }

        address, caption, cite, code, dfn, th, var {
            font-style: normal;
            font-weight: normal;
        }

        ol, ul {
            list-style: none;
        }

        caption, th {
            text-align: left;
        }

        h1, h2, h3, h4, h5, h6 {
            font-size: 100%;
            font-weight: normal;
        }

        q:before, q:after {
            content: '';
        }

        abbr, acronym {
            border: 0;
        }

        a {
            outline: none;
        }

        body:focus, div:focus, fieldset:focus, span:focus, li:focus, p:focus {
            outline: 0;
        }

        strong {
            font-weight: bold;
        }

        label {
            cursor: pointer;
        }
        /* End of reset*/
        body {
            min-width: 1250px;
            font-family: Arial,Verdana,Sans-serif;
            font-size: 12px;
            line-height: 1.5;
        }

        .cf:before, .cf:after {
            content: "";
            display: table;
        }

        .cf:after {
            clear: both;
        }

        .cf {
            *zoom: 1;
        }

        a, a:link, a:visited, a:hover, a:active {
            color: #105CB6;
            text-decoration: none;
            cursor: pointer;
        }

            a:hover, a:active, a:focus {
                text-decoration: none;
                color: #00f;
            }

        .wrapper {
            padding: 0 3% 50px;
        }

        .col {
            display: inline-block;
            vertical-align: top;
            width: 600px;
        }

        h1 {
            margin-bottom: 20px;
            padding: 20px 3% 5px;
            background-color: #3e3e3e;
            color: #fff;
            font-size: 24px;
        }

        h2 {
            margin-top: 30px;
            margin-bottom: 10px;
            font-size: 18px;
            line-height: 1.2;
        }

        h3 {
            margin-top: 30px;
            margin-bottom: 10px;
            font-size: 15px;
            line-height: 1.2;
        }

        h2 + h3 {
            margin-top: 20px;
        }

        h3 b {
            margin-right: 5px;
            padding: 5px 10px;
            background-color: #666;
            color: #fff;
            border-radius: 15px;
        }

        .warning {
            padding: 5px 10px;
            max-width: 1170px;
            background-color: #ffc;
        }
        /* form */
        .form-item {
            margin: 10px 0 10px 40px;
        }

            .form-item label {
                display: block;
            }

        .log {
            margin-top: 40px;
        }

        .error {
            padding-left: 5px;
        }

        input[type="text"], textarea, select {
            font-family: Arial,Verdana,Sans-serif;
            font-size: 12px;
            line-height: 1.2;
            padding: 3px;
            width: 250px;
        }

        textarea {
            width: 500px;
            height: 150px;
        }

        .log textarea {
            height: 650px;
            width: 530px;
        }

        select {
            width: 260px;
        }

        input[type="submit"] {
            font-family: Arial,Verdana,Sans-serif;
            font-size: 12px;
            line-height: 1.2;
            padding: 1px 5px;
            background-color: #dedede;
            color: #333;
            border: 1px solid #ccc;
            font-size: 11px;
            border-radius: 3px;
            cursor: pointer;
        }

        input:hover[type="submit"] {
            border-color: #333;
            background-color: #333;
            color: #fff;
        }

        input.save[type="submit"] {
            border-radius: 5px;
            font-size: 14px;
            font-weight: bold;
            padding: 10px 25px;
        }
        /* end of form */
    </style>
</head>
<body>
    <form id="form" runat="server">
        <asp:ScriptManager ID="ScriptManager" runat="server">
        </asp:ScriptManager>

        <h1>Welcome to Sitefinity 7.0 Related Data Migration assistant!</h1>
        <div class="wrapper">
            <p class="warning">
                <asp:Label ID="loginStatusLabel" runat="server"></asp:Label>
            </p>
            <div id="main-content" class="main-content">
                <div class="col">
                    <h2>Migration steps:</h2>

                    <h3><b>1</b> Manually create the new related data field</h3>
                    <h3><b>2</b> Select correct type of the migration</h3>
                    <div class="form-item">
                        <asp:Label ID="lblMigrationType" runat="server" AssociatedControlID="ddlMigrationType">Migration type</asp:Label>
                        <asp:DropDownList ID="ddlMigrationType" runat="server" AutoPostBack="true">
                            <asp:ListItem Text="-- Select migration type --" Value="-1"></asp:ListItem>
                            <asp:ListItem Text="Multiple Dynamic selector field (Guid Array)" Value="GuidArray"></asp:ListItem>
                            <asp:ListItem Text="Single Dynamic selector field (Guid)" Value="Guid"></asp:ListItem>
                            <asp:ListItem Text="Media field" Value="Media"></asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <h3><b>3</b> Fill the form with the required data for the migration</h3>
                    <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                        <ContentTemplate>
                            <div class="form-item">
                                <asp:Label ID="Label1" runat="server" AssociatedControlID="ddlParentItemTypeName">Parent item TypeName</asp:Label>
                                <asp:DropDownList ID="ddlParentItemTypeName" runat="server" AutoPostBack="true" DataValueField="Value" DataTextField="Text"></asp:DropDownList>
                            </div>
                            <div class="form-item">
                                <asp:Label ID="Label2" runat="server" AssociatedControlID="ddlParentItemProviderName">Parent item ProviderName</asp:Label>
                                <asp:DropDownList ID="ddlParentItemProviderName" runat="server" DataValueField="Name" DataTextField="Title"></asp:DropDownList>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                        <ContentTemplate>
                            <div class="form-item">
                                <asp:Label ID="Label3" runat="server" AssociatedControlID="ddlChildItemTypeName">Child item TypeName</asp:Label>
                                <asp:DropDownList ID="ddlChildItemTypeName" runat="server" AutoPostBack="true" DataValueField="Value" DataTextField="Text"></asp:DropDownList>
                            </div>
                            <div class="form-item">
                                <asp:Label ID="Label4" runat="server" AssociatedControlID="ddlChildItemProviderName">Child item ProviderName</asp:Label>
                                <asp:DropDownList ID="ddlChildItemProviderName" runat="server" DataValueField="Name" DataTextField="Title"></asp:DropDownList>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <div class="form-item">
                        <asp:Label ID="Label5" runat="server" AssociatedControlID="dynamicSelectorFieldNameTextBox">Dynamic items' selector/media FieldName (Old)</asp:Label>
                        <asp:TextBox ID="dynamicSelectorFieldNameTextBox" runat="server" placeholder="Old field name"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="RequiredFieldValidator5" ForeColor="Red" ControlToValidate="dynamicSelectorFieldNameTextBox" ValidationGroup="migration" ErrorMessage="Dynamic items' selector FieldName is Required" runat="server" CssClass="error" />
                    </div>
                    <div class="form-item">
                        <asp:Label ID="Label6" runat="server" AssociatedControlID="relatedDataFieldNameTextBox">Related data/media FieldName (New)</asp:Label>
                        <asp:TextBox ID="relatedDataFieldNameTextBox" runat="server" placeholder="Related field name"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="RequiredFieldValidator6" ForeColor="Red" ControlToValidate="relatedDataFieldNameTextBox" ValidationGroup="migration" ErrorMessage="Related data FieldName is Required" runat="server" CssClass="error" />
                    </div>
                    <h3><b>4</b> Click “Migrate data”</h3>
                    <div class="form-item">
                        <asp:Button ID="migrateDataBtn" runat="server" Text="Migrate data" CausesValidation="true" ValidationGroup="migration" Enabled="false" CssClass="save" />
                    </div>
                    <h3><b>5</b> Manually check if field values of several items are corresponding</h3>
                    <h3><b>6</b> Delete the old selector/media field</h3>
                </div>
                <div class="col">
                    <asp:UpdatePanel ID="UpdatePanel" runat="server">
                        <ContentTemplate>
                            <div class="form-item log">
                                <asp:Label runat="server" ID="lblMigrationLog" AssociatedControlID="MigrationLog">Migration log:</asp:Label>
                                <asp:TextBox runat="server" ID="MigrationLog" ReadOnly="true" TextMode="MultiLine" />
                                <asp:Timer ID="TimerStatusUpdate" runat="server" Interval="100" OnTick="TimerStatusUpdate_Tick" Enabled="false" />
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
            </div>
        </div>
    </form>
    <script type="text/javascript">
        window.onload = function () {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(EndRequestHandler);
        }

        function EndRequestHandler(sender, args) {
            var textarea = document.getElementById('<%=MigrationLog.ClientID %>');
            textarea.scrollTop = textarea.scrollHeight;
        }
    </script>
</body>
</html>
