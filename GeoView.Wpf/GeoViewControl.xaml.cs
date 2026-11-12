using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Resources;

namespace GeoView.Wpf
{
    public partial class GeoViewControl : UserControl
    {
        private readonly string _instanceId = Guid.NewGuid().ToString("N");
        private string ConfigHostName => $"appconfig-{_instanceId}.local";
        private string AssetsHostName => $"appassets-{_instanceId}.local";
        public event EventHandler<string>? ConfigFallbackUsed;
        public static readonly DependencyProperty ConfigPathProperty =
            DependencyProperty.Register(nameof(ConfigPath), typeof(string), typeof(GeoViewControl),
                new PropertyMetadata(null));

        public string? ConfigPath
        {
            get => (string?)GetValue(ConfigPathProperty);
            set => SetValue(ConfigPathProperty, value);
        }

        public static readonly DependencyProperty MapIdProperty =
            DependencyProperty.Register(nameof(MapId), typeof(string), typeof(GeoViewControl),
                new PropertyMetadata("mapOne"));

        public string MapId
        {
            get => (string)GetValue(MapIdProperty);
            set => SetValue(MapIdProperty, value);
        }

        public event EventHandler<string>? MapMessageReceived;
        public event EventHandler? MapReady;

        private string? _tempHtmlDir;

        public GeoViewControl()
        {
            InitializeComponent();
            Loaded += (_, _) => InitializeMapAsync();
        }

        private async void InitializeMapAsync()
        {
            await GeoViewWebView.EnsureCoreWebView2Async();

            _tempHtmlDir = Path.Combine(Path.GetTempPath(), "GeoViewWpf_" + _instanceId);
            Directory.CreateDirectory(_tempHtmlDir);
            ExtractEmbeddedHtml(Path.Combine(_tempHtmlDir, "GeoView.html"), ConfigHostName);

            GeoViewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                AssetsHostName, _tempHtmlDir, CoreWebView2HostResourceAccessKind.Allow);

            string configDir;
            if (!string.IsNullOrWhiteSpace(ConfigPath) && File.Exists(ConfigPath))
            {
                configDir = Path.GetDirectoryName(ConfigPath)!;
            }
            else
            {
                // Fall back to the embedded default config
                ConfigFallbackUsed?.Invoke(this, ConfigPath ?? "(none provided)");
                configDir = _tempHtmlDir; // reuse the same temp dir
                ExtractEmbeddedResource(
                    "Assets/config.default.json",
                    Path.Combine(_tempHtmlDir, "config.json"));
            }

            GeoViewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                ConfigHostName, configDir, CoreWebView2HostResourceAccessKind.Allow);

            GeoViewWebView.CoreWebView2.WebMessageReceived += (sender, args) =>
            {
                string? jsonMessage = args.TryGetWebMessageAsString();
                if (jsonMessage is null) return;

                if (jsonMessage.Contains("\"ready\""))
                {
                    MapReady?.Invoke(this, EventArgs.Empty);
                }

                MapMessageReceived?.Invoke(this, jsonMessage);
            };

            GeoViewWebView.Source = new Uri($"https://{AssetsHostName}/GeoView.html");
        }

        private static void ExtractEmbeddedHtml(string destinationPath, string configHostName)
        {
            string html = ReadEmbeddedResourceText("Assets/GeoView.html");
            html = html.Replace("{{CONFIG_HOST}}", configHostName);
            File.WriteAllText(destinationPath, html);
        }

        private static void ExtractEmbeddedResource(string resourceRelativePath, string destinationPath)
        {
            string text = ReadEmbeddedResourceText(resourceRelativePath);
            File.WriteAllText(destinationPath, text);
        }

        private static string ReadEmbeddedResourceText(string resourceRelativePath)
        {
            var uri = new Uri($"pack://application:,,,/GeoView.Wpf;component/{resourceRelativePath}");
            StreamResourceInfo info = Application.GetResourceStream(uri)
                ?? throw new FileNotFoundException($"Embedded resource not found: {resourceRelativePath}");

            using var reader = new StreamReader(info.Stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Executes an arbitrary expression against the cgpv MapViewer instance,
        /// e.g. "mapViewer.controllers.mapController.zoomToLonLatExtentOrCoordinate([...]);"
        /// </summary>
        public async System.Threading.Tasks.Task<string> ExecuteMapScriptAsync(string script)
        {
            string wrapped =
                $"(function() {{ const mapViewer = cgpv.api.getMapViewer('{MapId}'); {script} }})();";
            return await GeoViewWebView.ExecuteScriptAsync(wrapped);
        }

        // Convenience wrapper for a common operation
        public Task ZoomToAsync(double longitude, double latitude, double zoomLevel)
        {
            return ExecuteMapScriptAsync(
                $"mapViewer.controllers.mapController.zoomToLonLatExtentOrCoordinate([{longitude}, {latitude}], {{ zoom: {zoomLevel} }});");
        }
    }
}