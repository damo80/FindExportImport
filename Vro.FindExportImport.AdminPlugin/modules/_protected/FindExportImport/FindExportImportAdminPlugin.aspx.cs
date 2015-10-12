﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Castle.Core.Internal;
using EPiServer.PlugIn;
using EPiServer.Shell.WebForms;

namespace Vro.FindExportImport.AdminPlugin
{
    [GuiPlugIn(DisplayName = "Find Export / Import",
        Description = "Find Export/Import allows to export and import Find optimizations",
        Area = PlugInArea.AdminMenu, UrlFromModuleFolder = "FindExportImportAdminPlugin.aspx")]
    public partial class FindExportImportAdminPlugin : WebFormsBase
    {
        private ExportManager _exportManager;
        private ImportManager _importManager;

        protected override void OnInit(EventArgs e)
        {
            _importManager = new ImportManager();
            _exportManager = new ExportManager();

            CreateCheckBoxes(exporters, _exportManager.GetExporters().Select(exporter => exporter.EntityKey));
            CreateCheckBoxes(deleters, _importManager.GetImporters().Select(importer => importer.EntityKey));

            _exportManager.GetSites().ForEach(s => exportSite.Items.Add(new ListItem(s.Name, s.Id)));
            _exportManager.GetSites().ForEach(s => importSite.Items.Add(new ListItem(s.Name, s.Id)));
            _exportManager.GetSites().ForEach(s => deleteSite.Items.Add(new ListItem(s.Name, s.Id)));

            _exportManager.GetLanguages().ForEach(l => exportLanguage.Items.Add(new ListItem(l.Name, l.Id)));
            _exportManager.GetLanguages().ForEach(l => deleteLanguage.Items.Add(new ListItem(l.Name, l.Id)));

            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            importResults.Text = "";
            importResultsPanel.Visible = false;
            SystemMessageContainer.Heading = "Find Export / Import";
            
        }

        private void CreateCheckBoxes(Panel container, IEnumerable<string> ids)
        {
            var checkBoxes = ids
                .Select(id => new CheckBox
                {
                    ID = container.ID + id,
                    Text = Helpers.GetEntityName(id),
                    Checked = true
                });

            foreach (var checkBox in checkBoxes)
            {
                var wrapper = new Panel {CssClass = "epi-padding-small"};
                wrapper.Controls.Add(checkBox);
                container.Controls.Add(wrapper);
            }
        }

        private List<string> GetCheckedIds(Panel container)
        {
            var exportersList = new List<string>();

            foreach (Panel wrapper in container.Controls)
            {
                var checkBox = wrapper.Controls[0] as CheckBox;
                if (checkBox != null && checkBox.Checked)
                {
                    exportersList.Add(checkBox.ID.Substring(container.ID.Length));
                }
            }
            return exportersList;
        }

        protected void ExportClick(object sender, EventArgs e)
        {
            var exportersList = GetCheckedIds(exporters);
            Response.Clear();
            Response.ContentType = "applicaiton/json";
            Response.AddHeader("content-disposition", "attachment; filename=FindOptimizations.json");
            _exportManager.ExportToStream(exportersList, exportSite.SelectedValue, exportLanguage.SelectedValue, Response.OutputStream);
            Response.End();
        }

        protected void ImportClick(object sender, EventArgs e)
        {
            if (Request.Files.Count == 0 || Request.Files[0].ContentLength == 0)
            {
                SystemMessageContainer.Message = "No file selected for import";
                return;
            }

            var resultsMessage = _importManager.ImportFromStream(importSite.SelectedValue, Request.Files[0].InputStream);
            importResultsPanel.Visible = true;
            importResults.Text = !string.IsNullOrWhiteSpace(resultsMessage) ? resultsMessage.Replace(Environment.NewLine, "<br>") : "Import complete";
            
        }

        protected void DeleteClick(object sender, EventArgs e)
        {
            var deletersList = GetCheckedIds(deleters);
            _importManager.Delete(deletersList, deleteLanguage.SelectedValue, deleteSite.SelectedValue);
            deleteResultsPanel.Visible = true;
            deleteResults.Text = "Deletion complete";

        }
    }
}