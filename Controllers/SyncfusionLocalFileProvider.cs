
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using Syncfusion.Blazor.FileManager;


namespace filemanager.Server.Controllers
{
    [Route("api/[controller]")]
    public class SyncfusionLocalFileProviderController : Controller
    {
        public PhysicalFileProvider operation;
        public string basePath;
        string root = "wwwroot/FIles";
        [Obsolete]
        public SyncfusionLocalFileProviderController(IHostingEnvironment hostingEnvironment)
        {
            this.basePath = hostingEnvironment.ContentRootPath;
            this.operation = new PhysicalFileProvider();
            this.operation.RootFolder(this.basePath + this.root); 
        }
        /// <summary>
        /// This method is used for all the file operations like view, rename, etc
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        // Processing the File Manager operations.
        [Route("FileOperations")]
        public object FileOperations([FromBody] Syncfusion.Blazor.FileManager.Base.FileManagerDirectoryContent args)
        {
            switch (args.Action)
            {
                case "read":
                    return this.operation.ToCamelCase(this.operation.GetFiles(args.Path, args.ShowHiddenItems));
                case "delete":
                    return this.operation.ToCamelCase(this.operation.Delete(args.Path, args.Names));
                case "copy":
                    return this.operation.ToCamelCase(this.operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                case "move":
                    return this.operation.ToCamelCase(this.operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                case "details":
                    return this.operation.ToCamelCase(this.operation.Details(args.Path, args.Names));
                case "create":
                    return this.operation.ToCamelCase(this.operation.Create(args.Path, args.Name));
                case "search":
                    return this.operation.ToCamelCase(this.operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive));
                case "rename":
                    return this.operation.ToCamelCase(this.operation.Rename(args.Path, args.Name, args.NewName));
            }
            return null;
        }

        /// <summary>
        /// This method is responsible for downloading any file
        /// </summary>
        /// <param name="downloadInput"></param>
        /// <returns></returns>
        [Route("Download")]
        public IActionResult Download(string downloadInput)
        {
            Syncfusion.Blazor.FileManager.FileManagerDirectoryContent args = JsonConvert.DeserializeObject<Syncfusion.Blazor.FileManager.FileManagerDirectoryContent>(downloadInput);
            return operation.Download(args.Path, args.Names);
        }

        /// <summary>
        /// this method is used for uploading files
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uploadFiles"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        // Processing the Upload operation.
        [Route("Upload")]
        public IActionResult Upload(string path, IList<IFormFile> uploadFiles, string action)
        {
            Syncfusion.Blazor.FileManager.Base.FileManagerResponse uploadResponse;
            uploadResponse = operation.Upload(path, uploadFiles, action, null);
            if (uploadResponse.Error != null)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = Convert.ToInt32(uploadResponse.Error.Code);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = uploadResponse.Error.Message;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            return Content("");
        }

        [Route("GetImage")]
        public IActionResult GetImage(Syncfusion.Blazor.FileManager.FileManagerDirectoryContent args)
        {
            return this.operation.GetImage(args.Path, args.Id, false, null, null);
        }

    }
    



       
    }

    