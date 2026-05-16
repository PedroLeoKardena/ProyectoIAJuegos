// ExportPackage.cs
// Script de editor para exportar todos los assets del proyecto como un único .unitypackage
using UnityEngine;
using UnityEditor;

/// <summary>
/// Clase de utilidad para exportar el proyecto Unity completo como un paquete.
/// </summary>
public class ExportPackage
{
    /// <summary>
    /// Exporta todos los assets y dependencias del proyecto en un único .unitypackage.
    /// Accesible desde el menú Export → MyExport en el editor de Unity.
    /// </summary>
    [MenuItem("Export/MyExport")]
    static void export()
    {
        AssetDatabase.ExportPackage(
            AssetDatabase.GetAllAssetPaths(),
            PlayerSettings.productName + ".unitypackage",
            ExportPackageOptions.Interactive |
            ExportPackageOptions.Recurse |
            ExportPackageOptions.IncludeDependencies |
            ExportPackageOptions.IncludeLibraryAssets
        );
    }
}
