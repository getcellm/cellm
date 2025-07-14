using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class EnvironmentVariableEditorForm : Form
{
    private Dictionary<string, string?> _environmentVariables;

    public Dictionary<string, string?> EnvironmentVariables
    {
        get => _environmentVariables;
    }

    public EnvironmentVariableEditorForm(Dictionary<string, string?> environmentVariables)
    {
        InitializeComponent();
        _environmentVariables = new Dictionary<string, string?>(environmentVariables);
        PopulateList();
    }

    private void PopulateList()
    {
        environmentVariablesListView.Items.Clear();

        foreach (var kvp in _environmentVariables)
        {
            var item = new ListViewItem(kvp.Key);
            item.SubItems.Add(kvp.Value ?? "");
            environmentVariablesListView.Items.Add(item);
        }
    }

    private void newButton_Click(object sender, EventArgs e)
    {
        var form = new EnvironmentVariableEditForm("", "");
        if (form.ShowDialog() == DialogResult.OK)
        {
            var name = form.VariableName;
            var value = form.VariableValue;

            if (_environmentVariables.ContainsKey(name))
            {
                MessageBox.Show($"Environment variable '{name}' already exists.", "Duplicate Variable",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _environmentVariables[name] = value;
            PopulateList();
        }
    }

    private void editButton_Click(object sender, EventArgs e)
    {
        if (environmentVariablesListView.SelectedItems.Count == 0) return;

        var selectedItem = environmentVariablesListView.SelectedItems[0];
        var name = selectedItem.Text;
        var value = selectedItem.SubItems[1].Text;

        var form = new EnvironmentVariableEditForm(name, value);
        if (form.ShowDialog() == DialogResult.OK)
        {
            var newName = form.VariableName;
            var newValue = form.VariableValue;

            // If name changed, check for duplicates
            if (name != newName && _environmentVariables.ContainsKey(newName))
            {
                MessageBox.Show($"Environment variable '{newName}' already exists.", "Duplicate Variable",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Remove old entry if name changed
            if (name != newName)
            {
                _environmentVariables.Remove(name);
            }

            _environmentVariables[newName] = newValue;
            PopulateList();
        }
    }

    private void removeButton_Click(object sender, EventArgs e)
    {
        if (environmentVariablesListView.SelectedItems.Count == 0) return;

        var selectedItem = environmentVariablesListView.SelectedItems[0];
        var name = selectedItem.Text;

        var result = MessageBox.Show($"Remove environment variable '{name}'?", "Confirm Removal",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _environmentVariables.Remove(name);
            PopulateList();
        }
    }

    private void environmentVariablesListView_SelectedIndexChanged(object sender, EventArgs e)
    {
        bool hasSelection = environmentVariablesListView.SelectedItems.Count > 0;
        editButton.Enabled = hasSelection;
        removeButton.Enabled = hasSelection;
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void cancelButton_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}