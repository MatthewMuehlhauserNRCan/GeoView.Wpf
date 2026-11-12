# GeoView.Wpf

A reusable WPF control that embeds a Canadian Geospatial Platform (CGPV) / GeoView map inside a WebView2 host, with a simple C# API for configuration and map interaction.

## Features

- Drop-in `GeoViewControl` for WPF applications.
- Each instance manages its own isolated WebView2 document, safe to use multiple map controls in the same application.
- Point to your own `config.json` via the `ConfigPath` property, or let the control fall back to a built-in default configuration.
- Call arbitrary cgpv `MapViewer` API methods directly from C# via `ExecuteMapScriptAsync`.
- `MapReady` and `MapMessageReceived` events for integrating map lifecycle and JS-to-C# messaging into your app.

## Prerequisites

- .NET 10 (Windows), WPF application.
- Microsoft Edge WebView2 Runtime installed (bundled automatically via the WebView2.Runtime.X64 NuGet dependency, or pre-installed with Windows 11 / recent Windows updates).

## Installation

```shell
dotnet add package GeoView.Wpf
```

## Getting Started

### 1. Add the control to your XAML

```xml
<Window xmlns:geo="clr-namespace:GeoView.Wpf;assembly=GeoView.Wpf">
```

Inside your layout:

```xml
<geo:GeoViewControl x:Name="GeoView"
                     ConfigPath="C:\MyApp\MapConfig\config.json"
                     MapId="mapOne"
                     MapReady="GeoView_MapReady"
                     MapMessageReceived="GeoView_MapMessageReceived" />
```

### 2. Provide your own config.json (optional)

`GeoViewControl` ships with a minimal default configuration so the map renders out of the box. To customize the basemap, layers, projection, or other map settings, point `ConfigPath` at your own GeoView-compatible `config.json`:

```csharp
GeoView.ConfigPath = @"C:\MyApp\MapConfig\config.json";
```

If `ConfigPath` is null, empty, or points to a file that doesn't exist, the control automatically falls back to its embedded default config and raises the `ConfigFallbackUsed` event, so you can detect and log misconfiguration during development.

See the GeoView configuration reference at [github.com/Canadian-Geospatial-Platform/geoview](https://github.com/Canadian-Geospatial-Platform/geoview) for the full schema (basemap options, layers, view settings, etc.).

### 3. Interact with the map

Once `MapReady` fires, the underlying cgpv `MapViewer` instance is available and you can call any of its API methods using `ExecuteMapScriptAsync`:

```csharp
private async void GeoView_MapReady(object? sender, EventArgs e)
{
    await GeoView.ExecuteMapScriptAsync(
        "mapViewer.controllers.mapController.zoomToLonLatExtentOrCoordinate([-79.38, 43.65], { zoom: 13 });");
}
```

Common operations are also available as convenience wrappers:

```csharp
await GeoView.ZoomToAsync(-79.3832, 43.6532, 13);
```

### 4. Handle messages sent from the map (JS to C#)

```csharp
private void GeoView_MapMessageReceived(object? sender, string jsonMessage)
{
    // jsonMessage is whatever payload was posted from JavaScript via
    // window.chrome.webview.postMessage(...)
}
```

## Multiple Maps in One Application

Each `GeoViewControl` instance creates its own isolated WebView2 document and virtual host mappings, so you can safely place multiple controls, each with a different `ConfigPath`, in the same window or across multiple windows without conflicts.

> **Note:** `MapId` only needs to be unique if you later embed multiple map containers within a single HTML document. For the standard one-control-per-map scenario, the default `MapId` (`"mapOne"`) is safe to reuse across controls.

## API Reference

| Member | Type | Description |
|---|---|---|
| `ConfigPath` | `string`, DependencyProperty | Full path to a GeoView `config.json`. Falls back to a built-in default if not set or not found. |
| `MapId` | `string`, DependencyProperty | The id of the map container element in the HTML template. Defaults to `"mapOne"`. |
| `MapReady` | `event EventHandler` | Raised once `cgpv.init()` completes and the `MapViewer` is available. |
| `MapMessageReceived` | `event EventHandler<string>` | Raised when JavaScript posts a message back to the host via WebView2. |
| `ConfigFallbackUsed` | `event EventHandler<string>` | Raised when the embedded default config is used instead of a user-supplied `ConfigPath`. |
| `ExecuteMapScriptAsync(string script)` | `Task<string>` | Executes arbitrary JavaScript in the context of the map's `mapViewer` instance. |
| `ZoomToAsync(double lon, double lat, double zoom)` | `Task` | Convenience wrapper around `zoomToLonLatExtentOrCoordinate`. |

## License

MIT
