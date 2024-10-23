﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ABT.Test.Exec.AppConfig {
    public partial class SelectTests : Form {
        public String Selection { get; private set; }
        internal readonly Dictionary<String, Operation> Operations;
        internal readonly Dictionary<String, Group> Groups;

        public SelectTests(Dictionary<String, Operation> testOperations, Dictionary<String, Group> testGroups) {
            InitializeComponent();
            Operations = testOperations;
            Groups = testGroups;
            ListSelections.MultiSelect = false;
            radioButtonTestOperations.Checked = true;
            radioButtonTestGroups.Checked = false;
            ListViewRefresh();
            FormRefresh();
        }

        private void ListViewRefresh() {
            ListSelections.Clear();
            ListSelections.View = View.Details;
            ListSelections.Columns.Add("ID");
            ListSelections.Columns.Add("Description");
        }

        private void FormRefresh() {
            if (radioButtonTestOperations.Checked) foreach (KeyValuePair<String, Operation> kvp in Operations) ListSelections.Items.Add(new ListViewItem(new String[] { kvp.Key, kvp.Value.Description }));
            else foreach (KeyValuePair<String, Group> kvp in Groups) if (kvp.Value.Selectable) ListSelections.Items.Add(new ListViewItem(new String[] { kvp.Key, kvp.Value.Description }));
            ListSelections.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent);
            ListSelections.Columns[1].Width = -2;
            // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.columnheader.width?redirectedfrom=MSDN&view=windowsdesktop-7.0#System_Windows_Forms_ColumnHeader_Width
            ListSelections.ResetText();
            OK.Enabled = false;
        }

        private void OK_Click(Object sender, EventArgs e) {
            if (ListSelections.SelectedItems.Count == 1) {
                Selection = ListSelections.SelectedItems[0].Text;
                DialogResult = DialogResult.OK;
            }
        }

        private void List_MouseDoubleClick(Object sender, MouseEventArgs e) { OK_Click(sender, e); }

        public static (String TestElementID, Boolean IsOperation) Get(Dictionary<String, Operation> testOperations, Dictionary<String, Group> testGroups) {
            SelectTests selectTests = new SelectTests(testOperations, testGroups);
            selectTests.ShowDialog(); // Waits until user clicks OK button.
            String testElementID = selectTests.ListSelections.SelectedItems[0].Text;
            Boolean isOperation = selectTests.radioButtonTestOperations.Checked;
            selectTests.Dispose();
            return (testElementID, isOperation);
        }

        private void List_SelectionChanged(Object sender, ListViewItemSelectionChangedEventArgs e) { OK.Enabled = true; }

        private void GroupBoxSelect_CheckedChanged(Object sender, EventArgs e) {
            if (((RadioButton)sender).Checked) { // Do stuff only if the radio button is checked (or the action will run twice).
                ListViewRefresh();
                FormRefresh();
            }
        }

        private void SelectTests_Load(Object sender, EventArgs e) { }
    }
}
