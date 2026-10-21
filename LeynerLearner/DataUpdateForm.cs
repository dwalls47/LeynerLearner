using LeynerLearner.Core;

namespace LeynerLearner
{
    /// <summary>
    /// The "Update data" window: shows which data the app is using, downloads or opens a data pack, and installs it
    /// or goes back to the built-in data. <paramref name="replaceData"/> runs a change to the installed data while the
    /// main window lets go of it, then reloads.
    /// </summary>
    public class DataUpdateForm : Form
    {
        readonly Func<DataStore.Choice> current;
        readonly Action<Action> replaceData;
        readonly Label lblStatus = new() { Dock = DockStyle.Top, AutoSize = false, Height = 80, Padding = new Padding(8) };
        readonly TextBox txtLog = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true };
        readonly Button btnDownload = new() { Text = "Download update", AutoSize = true };
        readonly Button btnFile = new() { Text = "Install from file…", AutoSize = true };
        readonly Button btnBuiltIn = new() { Text = "Use built-in data", AutoSize = true };
        readonly Button btnClose = new() { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
        CancellationTokenSource? busy;

        public DataUpdateForm(Func<DataStore.Choice> current, Action<Action> replaceData)
        {
            this.current = current;
            this.replaceData = replaceData;
            Text = "Update data";
            Icon = AppIcon.Value;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(640, 420);
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            CancelButton = btnClose;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(4), FlowDirection = FlowDirection.LeftToRight };
            buttons.Controls.AddRange([btnDownload, btnFile, btnBuiltIn, btnClose]);
            Controls.Add(txtLog);
            Controls.Add(lblStatus);
            Controls.Add(buttons);

            btnDownload.Click += async (_, _) => await Update(DataStore.DownloadAsync);
            btnFile.Click += async (_, _) =>
            {
                using var dlg = new OpenFileDialog { Filter = "Data packs (*.zip)|*.zip", Title = "Install data pack" };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    await Update((progress, cancel) => DataStore.StageAsync(dlg.FileName, progress, cancel));
            };
            btnBuiltIn.Click += (_, _) => UseBuiltIn();
            btnClose.Click += (_, _) => busy?.Cancel();
            FormClosing += (_, e) => { if (busy != null) { busy.Cancel(); e.Cancel = true; } };

            Log($"Updates are downloaded from {DataStore.PackUrl}");
            ShowStatus();
        }

        void ShowStatus()
        {
            var c = current();
            string version = c.Manifest?.PackVersion is { Length: > 0 } v ? $"version {v}" : "no manifest";
            lblStatus.Text = c.IsInstalled
                ? $"Using updated data ({version}) from\n{c.Dir}"
                : $"Using the built-in data ({version}) from\n{c.Dir}" +
                  (c.InstalledProblem != null ? $"\n\nThe updated data in {DataStore.InstalledDir} is not used: {c.InstalledProblem}." : "");
            btnBuiltIn.Enabled = busy == null && Directory.Exists(DataStore.InstalledDir);
        }

        void Log(string s) => txtLog.AppendText((txtLog.TextLength > 0 ? Environment.NewLine : "") + s);

        void SetBusy(CancellationTokenSource? cts)
        {
            busy = cts;
            btnDownload.Enabled = btnFile.Enabled = cts == null;
            btnClose.Text = cts == null ? "Close" : "Cancel";
            UseWaitCursor = cts != null;
            ShowStatus();
        }

        async Task Update(Func<IProgress<string>, CancellationToken, Task<DataStore.StagedPack>> stage)
        {
            using var cts = new CancellationTokenSource();
            SetBusy(cts);
            DataStore.StagedPack pack;
            try
            {
                pack = await stage(new Progress<string>(s => lblStatus.Text = s), cts.Token);
            }
            catch (OperationCanceledException)
            {
                Log("Cancelled.");
                return;
            }
            catch (Exception e)
            {
                Log("Could not get the data pack: " + e.Message);
                return;
            }
            finally
            {
                SetBusy(null);
            }

            var was = current().Manifest;
            var changes = DataStore.Changes(was, pack.Manifest);
            string summary = $"Data pack version {pack.Manifest.PackVersion} (now using {was?.PackVersion ?? "unknown"}):" +
                             Environment.NewLine + string.Join(Environment.NewLine, changes.Select(l => "  " + l));
            Log(summary);
            if (string.CompareOrdinal(pack.Manifest.PackVersion, was?.PackVersion ?? "") < 0 &&
                MessageBox.Show(this, $"This pack (version {pack.Manifest.PackVersion}) is older than the data in use (version {was?.PackVersion}). " +
                                      "Install it anyway? (The app will use it only until it is older than the built-in data.)", Text,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes
                || MessageBox.Show(this, summary + Environment.NewLine + Environment.NewLine + "Install it?", Text,
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                DataStore.Discard(pack);
                Log("Not installed.");
                return;
            }

            try
            {
                replaceData(() => DataStore.Install(pack));
                Log($"Installed version {pack.Manifest.PackVersion}.");
            }
            catch (Exception e)
            {
                DataStore.Discard(pack);
                Log("Could not install the data pack: " + e.Message);
            }
            ShowStatus();
        }

        void UseBuiltIn()
        {
            if (MessageBox.Show(this, $"Delete the updated data in {DataStore.InstalledDir} and use the data that came with the app?", Text,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var was = current().Manifest;
            try
            {
                replaceData(DataStore.RemoveInstalled);
                var now = current().Manifest;
                Log("Now using the built-in data" + (now != null ? $" (version {now.PackVersion}):" + Environment.NewLine +
                    string.Join(Environment.NewLine, DataStore.Changes(was, now).Select(l => "  " + l)) : "."));
            }
            catch (Exception e)
            {
                Log("Could not remove the updated data: " + e.Message);
            }
            ShowStatus();
        }
    }
}
