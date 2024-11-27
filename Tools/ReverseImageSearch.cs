using ImgComparer.Model;
using System;
using System.Diagnostics;
using System.IO;

namespace ImgComparer.Tools
{
    public static class ReverseImageSearch
    {
        public static void Open(Image image)
        {
            string url = "https://saucenao.com/search.php";

            // Base64 encode the image (needed to embed it in JavaScript)
            string base64Image = Convert.ToBase64String(File.ReadAllBytes(image.path));
            string htmlContent = $@"
<!DOCTYPE html>
<html>
<body onload='upload()'>
    <form id='searchForm' action='{url}' method='post' enctype='multipart/form-data'>
        <input id='fileInput' type='file' name='file' name='file' style='display:none;' />
        <input type='submit' value='Submit' />
    </form>
    <script>
		const fileInput = document.querySelector('input[type=file]');
        const base64Data = '{base64Image}';
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length).fill(0).map((_, i) => byteCharacters.charCodeAt(i));
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], {{ type: 'image/png' }}); // Set correct MIME type
        const file = new File([blob], '{image.Filename}', {{ type: 'image/png' }}); // File must also have the MIME type
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        fileInput.files = dataTransfer.files;

        function upload() {{
            document.getElementById('searchForm').submit();
		}}
    </script>
</body>
</html>";

            // Save HTML to a temporary file
            string tempFile = Path.Combine(Path.GetTempPath(), "upload.html");
            File.WriteAllText(tempFile, htmlContent);

            // Open the temporary file in the default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
    }
}
