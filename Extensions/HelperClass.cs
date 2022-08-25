using CryptoSysPKI;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

namespace FileManagerSample.Extensions
{
    public class HelperClass
    {
        public bool FileExists(string rootpath, string filename)
        {
            if (System.IO.File.Exists(Path.Combine(rootpath, filename)))
                return true;

            foreach (string subDir in Directory.GetDirectories(rootpath, "*", SearchOption.AllDirectories))
            {
                if (System.IO.File.Exists(Path.Combine(subDir, filename)))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Create UUID using RFX //
        /// </summary>
        /// <returns></returns>
        public string Create_UUID()
        {
            //                                           12345678 9012 3456 7890 123456789012
            // Returns a 36-character string in the form XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
            // where "X" is an "upper-case" hexadecimal digit [0-9A-F].
            // Use the LCase function if you want lower-case letters.

            byte[] abData = null;
            string strHex = null;

            // 1. Generate 16 random bytes = 128 bits
            abData = Rng.Bytes(16);
            // DEBUGGING...
            //'Console.WriteLine("RNG=" & Cnv.ToHex(abData))

            // 2. Adjust certain bits according to RFC 4122 section 4.4.
            // This just means do the following
            // (a) set the high nibble of the 7th byte equal to 4 and
            // (b) set the two most significant bits of the 9th byte to 10'B,
            //     so the high nibble will be one of {8,9,A,B}.
            abData[6] = (byte)(0x40 | ((int)abData[6] & 0xf));
            abData[8] = (byte)(0x80 | ((int)abData[8] & 0x3f));

            // 3. Convert the adjusted bytes to hex values
            strHex = Cnv.ToHex(abData);
            // DEBUGGING...
            //'Console.WriteLine("ADJ=" & Cnv.ToHex(abData))
            //'Console.WriteLine("                ^   ^") ' point to the nibbles we've changed

            // 4. Add four hyphen '-' characters
            //'strHex = Left$(strHex, 8) & "-" & Mid$(strHex, 9, 4) & "-" & Mid$(strHex, 13, 4) _
            //'    & "-" & Mid$(strHex, 17, 4) & "-" & Right$(strHex, 12)
            strHex = strHex.Substring(0, 8) + "-" + strHex.Substring(8, 4) + "-" + strHex.Substring(12, 4) + "-" + strHex.Substring(16, 4) + "-" + strHex.Substring(20, 12);

            // Return the UUID string
            return strHex;
        }

        /// <summary>
        /// Compress the PDF file Using Syncfusion.Pdf.Parsing package
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public MemoryStream CompressPDF(string path,string fileName)
        {
            FileStream inputDocument = new FileStream(path, FileMode.Open, FileAccess.Read);
            //Load an existing PDF document
            PdfLoadedDocument loadedDocument = new PdfLoadedDocument(inputDocument);
            loadedDocument.FileStructure.IncrementalUpdate = false;
            //Create a new compression option.
            PdfCompressionOptions options = new PdfCompressionOptions();
            //Enable the compress image.
            options.CompressImages = true;
            //Set the image quality.
            options.ImageQuality = 30;
            options.OptimizeFont = true;
            //Remove metadata from the PDF document
            options.RemoveMetadata = true;
            options.OptimizePageContents = true;
            //Flatten form fields in the PDF document
            if (loadedDocument.Form != null)
                loadedDocument.Form.Flatten = true;
            //Flatten all the annotations in the PDF document
            foreach (PdfPageBase page in loadedDocument.Pages)
            {
                if (page.Annotations != null)
                    page.Annotations.Flatten = true;
            }
            //Assign the compression option and compress the PDF document
            loadedDocument.Compress(options);
            //Create a MemoryStream instance to save the document
            MemoryStream outputDocument = new MemoryStream();
            //Save the PDF document
            loadedDocument.Save(outputDocument);
            outputDocument.Position = 0;
            //Close the document
            loadedDocument.Close(true);
            //Download the PDF document in the browser.
            FileStreamResult fileStreamResult = new FileStreamResult(outputDocument, "application/pdf");
            fileStreamResult.FileDownloadName = fileName;
            return outputDocument;
        }


    }
}
