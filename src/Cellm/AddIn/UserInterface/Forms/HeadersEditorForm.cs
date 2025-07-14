using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class HeadersEditorForm : Form
{
    private Dictionary<string, string> _headers;

    public Dictionary<string, string> Headers
    {
        get => _headers;
    }

    public HeadersEditorForm(Dictionary<string, string> headers)
    {
        InitializeComponent();
        _headers = new Dictionary<string, string>(headers);
        PopulateList();
    }

    private void PopulateList()
    {
        headersListView.Items.Clear();

        foreach (var kvp in _headers)
        {
            var item = new ListViewItem(kvp.Key);
            item.SubItems.Add(kvp.Value);
            headersListView.Items.Add(item);
        }
    }

    private void newButton_Click(object sender, EventArgs e)
    {
        var form = new HeaderEditForm("", "");
        if (form.ShowDialog() == DialogResult.OK)
        {
            var name = form.HeaderName;
            var value = form.HeaderValue;

            if (_headers.ContainsKey(name))
            {
                MessageBox.Show($"Header '{name}' already exists.", "Duplicate Header",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _headers[name] = value;
            PopulateList();
        }
    }

    private void editButton_Click(object sender, EventArgs e)
    {
        if (headersListView.SelectedItems.Count == 0) return;

        var selectedItem = headersListView.SelectedItems[0];
        var name = selectedItem.Text;
        var value = selectedItem.SubItems[1].Text;

        var form = new HeaderEditForm(name, value);
        if (form.ShowDialog() == DialogResult.OK)
        {
            var newName = form.HeaderName;
            var newValue = form.HeaderValue;

            // If name changed, check for duplicates
            if (name != newName && _headers.ContainsKey(newName))
            {
                MessageBox.Show($"Header '{newName}' already exists.", "Duplicate Header",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Remove old entry if name changed
            if (name != newName)
            {
                _headers.Remove(name);
            }

            _headers[newName] = newValue;
            PopulateList();
        }
    }

    private void removeButton_Click(object sender, EventArgs e)
    {
        if (headersListView.SelectedItems.Count == 0) return;

        var selectedItem = headersListView.SelectedItems[0];
        var name = selectedItem.Text;

        var result = MessageBox.Show($"Remove header '{name}'?", "Confirm Removal",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _headers.Remove(name);
            PopulateList();
        }
    }

    private void headersListView_SelectedIndexChanged(object sender, EventArgs e)
    {
        bool hasSelection = headersListView.SelectedItems.Count > 0;
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