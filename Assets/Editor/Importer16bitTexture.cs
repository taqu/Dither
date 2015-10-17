
// userData DITHER16 のみディザリング

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text;

class Importer16bitTexture : AssetPostprocessor
{
    const string Tag = "DITHER16";

    private TextureImporterFormat formatBackup;

    void OnPreprocessTexture()
    {
        TextureImporter importer = assetImporter as TextureImporter;

        formatBackup = importer.textureFormat;

        if(importer.userData != Tag){
            return;
        }
        importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
    }

    void OnPostprocessTexture(Texture2D texture)
    {
        TextureImporter importer = assetImporter as TextureImporter;
        if(importer.userData != Tag){
            return;
        }

        ErrorDifusion errorDiffusion = new ErrorDifusion();
        errorDiffusion.convert(texture);

        EditorUtility.CompressTexture(texture, TextureFormat.RGBA4444, TextureCompressionQuality.Best);

        //byte[] bytes = texture.EncodeToPNG();
        //string name = Application.dataPath + "/../" + texture.name + importer.userData + ".png";
        //System.IO.File.WriteAllBytes(name, bytes);
        importer.textureFormat = TextureImporterFormat.Automatic16bit;
    }
}
