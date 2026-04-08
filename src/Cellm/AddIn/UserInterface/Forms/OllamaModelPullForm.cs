using OllamaSharp;
using OllamaSharp.Models;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class OllamaModelPullForm : Form
{
    private readonly OllamaApiClient _client;
    private readonly string _modelName;
    private CancellationTokenSource? _cts;

    public bool PullSucceeded { get; private set; }

    public OllamaModelPullForm(OllamaApiClient client, string modelName)
    {
        InitializeComponent();

        _client = client;
        _modelName = modelName;

        statusLabel.Text = $"Downloading {modelName}...";
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        _cts = new CancellationTokenSource();

        try
        {
            var request = new PullModelRequest { Model = _modelName };

            await foreach (var response in _client.PullModelAsync(request, _cts.Token))
            {
                if (response?.Percent > 0)
                {
                    progressBar.Value = Math.Min((int)response.Percent, 100);
                    statusLabel.Text = $"Downloading {_modelName}... {(int)response.Percent}%";
                }
                else
                {
                    statusLabel.Text = $"Downloading {_modelName}...";
                }
            }

            PullSucceeded = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException)
        {
            PullSucceeded = false;
            DialogResult = DialogResult.Cancel;
            Close();
        }
        catch (Exception ex)
        {
            PullSucceeded = false;
            MessageBox.Show($"Failed to download model: {ex.Message}", "Cellm", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void cancelButton_Click(object sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Dispose();
        }

        base.Dispose(disposing);
    }
}
