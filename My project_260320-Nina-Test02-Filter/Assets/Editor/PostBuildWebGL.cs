using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class PostBuildWebGL : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        var outputPath = report.summary.outputPath;
        var indexPath = Path.Combine(outputPath, "index.html");

        if (!File.Exists(indexPath))
            return;

        var html = File.ReadAllText(indexPath);

        html = InjectHideUnityFooterCss(html);
        html = RemoveUnityTitle(html);

        File.WriteAllText(indexPath, html);
    }

    static string InjectHideUnityFooterCss(string html)
    {
        const string css = "<style>#unity-footer,#unity-logo,#unity-webgl-logo,#unity-fullscreen-button,#unity-build-title{display:none!important;}</style>";

        if (html.Contains("#unity-footer"))
            return html;

        var headClose = "</head>";
        var insert = css + "\n" + headClose;
        if (html.Contains(headClose))
            return html.Replace(headClose, insert);

        return html;
    }

    static string RemoveUnityTitle(string html)
    {
        // Replace the default Unity title if present
        const string unityTitlePrefix = "<title>Unity WebGL Player |";
        if (html.Contains(unityTitlePrefix))
        {
            var start = html.IndexOf(unityTitlePrefix);
            if (start >= 0)
            {
                var end = html.IndexOf("</title>", start);
                if (end > start)
                {
                    var oldTitle = html.Substring(start, end - start + "</title>".Length);
                    html = html.Replace(oldTitle, "<title></title>");
                }
            }
        }

        return html;
    }
}
