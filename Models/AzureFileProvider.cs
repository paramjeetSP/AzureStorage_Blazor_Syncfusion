using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Syncfusion.EJ2.FileManager.Base;
using System.Text;
using FileManagerDirectoryContent = Syncfusion.EJ2.FileManager.Base.FileManagerDirectoryContent;
using ErrorDetails = Syncfusion.EJ2.FileManager.Base.ErrorDetails;
using FileDetails = Syncfusion.EJ2.FileManager.Base.FileDetails;

namespace FileManagerSample.Models
{
    public class AzureFileProvider
    {
        List<FileManagerDirectoryContent> directoryContentItems = new List<FileManagerDirectoryContent>();
        BlobContainerClient container;
        string pathValue;
        string blobPath;
        string filesPath;
        [Obsolete]
        private Microsoft.Extensions.Hosting.IHostingEnvironment _hostingEnvironment;
        long size;
        string rootPath;
        string currentFolderName = "";
        string previousFolderName = "";
        string initialFolderName = "";
        List<string> existFiles = new List<string>();
        List<string> missingFiles = new List<string>();
        bool isFolderAvailable = false;
        public string AzureFileShare = "test2";
        List<FileManagerDirectoryContent> copiedFiles = new List<FileManagerDirectoryContent>();
        DateTime lastUpdated = DateTime.MinValue;
        DateTime prevUpdated = DateTime.MinValue;
        public string res;
        CloudStorageAccount storageAccount;
        CloudFileClient cloudFileClient;
        CloudFileShare fileShare;
        CloudFileDirectory fileDirectory;

        [Obsolete]
        public AzureFileProvider(IConfiguration configuration,
            Microsoft.Extensions.Hosting.IHostingEnvironment
           hostingEnvironment1)
        {
            _hostingEnvironment = hostingEnvironment1;
            res = configuration.GetSection("fileShareConnectionString").Value;

        }

        public async Task<CloudFileDirectory> AzureShareConfigSettingsAsync()
        {
            storageAccount = CloudStorageAccount.Parse(res);
            cloudFileClient = storageAccount.CreateCloudFileClient();
            fileShare = cloudFileClient.GetShareReference(AzureFileShare);
            await fileShare.CreateIfNotExistsAsync();
            await fileShare.ExistsAsync();
            fileDirectory = fileShare.GetRootDirectoryReference();
            return fileDirectory;
        }

        // Sets blob and file path
        public void SetBlobContainer(string blob_Path, string file_Path)
        {
            blobPath = blob_Path;
            filesPath = file_Path;
            rootPath = filesPath.Replace(blobPath, "");
        }

        // Reads the storage 
        public async Task<FileManagerResponse> GetAzureSMBFilesAsync(string path, 
            params FileManagerDirectoryContent[] selectedItems)
        {
            fileDirectory = await AzureShareConfigSettingsAsync();
            return await downloadCloudFilesAsync(fileDirectory, path,selectedItems);
        }

        // Reads the storage files

        /// <summary>
        /// Downloads Directories, Sub drectories and Files from Azure Storage files Share//
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<FileManagerResponse> downloadCloudFilesAsync(CloudFileDirectory rootDir,
           string azureFolderPath, params FileManagerDirectoryContent[] selectedItems)
        {
            try
            {
                List<IListFileItem> results = new List<IListFileItem>();
                FileContinuationToken? token = null;
                List<FileManagerDirectoryContent> details = new List<FileManagerDirectoryContent>();
                FileManagerResponse readResponse = new FileManagerResponse();
                FileManagerDirectoryContent cwd = new FileManagerDirectoryContent();
                if (azureFolderPath == "/" || string.IsNullOrEmpty(azureFolderPath)) // Root Path
                {
                    cwd.Name = selectedItems.Length > 0 ? selectedItems[0].Name : azureFolderPath;
                    cwd.Type = "File Folder";
                    cwd.FilterPath = selectedItems.Length > 0 ? selectedItems[0].FilterPath : "";
                    cwd.Size = 0;
                    // Getting the root files data and Folders//
                    do
                    {
                        FileResultSegment resultSegment =
                            rootDir.ListFilesAndDirectoriesSegmentedAsync(token).GetAwaiter().GetResult();
                        results.AddRange(resultSegment.Results);
                        token = resultSegment.ContinuationToken;
                    }
                    while (token != null);
                    foreach (IListFileItem listItem in results)
                    {
                        if (listItem is CloudFile file)
                        {
                            //get the cloudfile's propertities and metadata 
                            await file.FetchAttributesAsync();
                            string FileName = file.Name;
                            FileManagerDirectoryContent entry = new FileManagerDirectoryContent();
                            entry.Name = file.Name;
                            entry.Type = System.IO.Path.GetExtension(file.Name);
                            entry.IsFile = true;
                            entry.Size = file.Properties.Length / 1024;
                            entry.DateModified = DateTime.Now;
                            entry.HasChild = false;
                            entry.FilterPath = selectedItems.Length > 0 ? azureFolderPath.Replace("CloudFiles", "") : "/";
                            details.Add(entry);

                        }
                        else if (listItem is CloudFileDirectory dir)
                        {
                            FileManagerDirectoryContent entry = new FileManagerDirectoryContent();
                            entry.Name = dir.Name;//.Replace(azureFolderPath, "").Replace("/", "");
                            entry.Type = "Directory";
                            entry.IsFile = false;
                            entry.Size = 0;
                            entry.HasChild = false;
                            entry.FilterPath = azureFolderPath;
                            entry.FilterPath = selectedItems.Length > 0 ? azureFolderPath.Replace("CloudFiles", "") : "/";
                            entry.DateModified = DateTime.Now;
                            lastUpdated = prevUpdated = DateTime.MinValue;
                            details.Add(entry);
                        }
                    }
                    cwd.HasChild = false;
                    readResponse.CWD = cwd;
                    readResponse.Files = details;
                    return readResponse;
                }
                else
                {
                    string newpath = selectedItems[0].FilterPath.Remove(0, 1);
                    string finalPath = newpath + selectedItems[0].Name + "/";
                    cwd.Name = selectedItems.Length > 0 ? selectedItems[0].Name : azureFolderPath;
                    cwd.Type = "File Folder";
                    cwd.FilterPath = selectedItems.Length > 0 ? selectedItems[0].FilterPath : "";
                    cwd.Size = 0;
                    //getting the Sub Directories and files based on the Path//
                    //string CurrentPath = String.Empty;
                    //if (azureFolderPath.Contains("/") == true)
                    //{
                    //    CurrentPath = azureFolderPath.Remove(0, 1);

                    //}
                    //else
                    //{
                    //    CurrentPath = finalPath;
                    //}
                    var destDir = rootDir.GetDirectoryReference(finalPath);
                    do
                    {
                        FileResultSegment resultSegment =
                            destDir.ListFilesAndDirectoriesSegmentedAsync(token).GetAwaiter().GetResult();
                        results.AddRange(resultSegment.Results);
                        token = resultSegment.ContinuationToken;
                    }
                    while (token != null);
                    foreach (IListFileItem listItem in results)
                    {
                        if (listItem is CloudFile file)
                        {
                            //get the cloudfile's propertities and metadata 
                            await file.FetchAttributesAsync();
                            string FileName = file.Name;
                            FileManagerDirectoryContent entry = new FileManagerDirectoryContent();
                            entry.Name = file.Name;
                            entry.Type = System.IO.Path.GetExtension(file.Name);
                            entry.IsFile = true;
                            entry.Size = file.Properties.Length / 1024;
                            entry.DateModified = DateTime.Now;
                            entry.HasChild = false;
                            entry.FilterPath = selectedItems.Length > 0 ? azureFolderPath.Replace("CloudFiles", "") : "/"; //azureFolderPath;
                            details.Add(entry);

                        }
                        else if (listItem is CloudFileDirectory dir)
                        {
                            FileManagerDirectoryContent entry = new FileManagerDirectoryContent();
                            entry.Name = dir.Name;
                            entry.Type = "Directory";
                            entry.IsFile = false;
                            entry.Size = 0;
                            entry.HasChild = false;
                            entry.FilterPath = selectedItems.Length > 0 ? azureFolderPath.Replace("CloudFiles", "") : "/";
                            entry.DateModified = DateTime.Now;
                            lastUpdated = prevUpdated = DateTime.MinValue;
                            details.Add(entry);
                        }
                    }
                    cwd.HasChild = true;
                    readResponse.CWD = cwd;
                    if(details.Count()==0)
                    {
                        FileManagerDirectoryContent entry = new FileManagerDirectoryContent();
                        entry.Name = selectedItems[0].Name;
                        entry.Type = "Directory";
                        entry.IsFile = false;
                        entry.ShowHiddenItems = true;
                        entry.Size = 0;
                        entry.HasChild = false;
                        entry.FilterPath = selectedItems.Length > 0 ? azureFolderPath.Replace("CloudFiles", "") : "/";
                        entry.DateModified = DateTime.Now;
                        lastUpdated = prevUpdated = DateTime.MinValue;
                        details.Add(entry);
                    }
                    readResponse.Files = details;
                    return readResponse;

                }
#pragma warning disable CS0162 // Unreachable code detected
                return readResponse;
#pragma warning restore CS0162 // Unreachable code detected
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        // Returns the last modified date for directories
        protected async Task<DateTime> DirectoryLastModified(string path)
        {
            foreach (Azure.Page<BlobItem> page in container.GetBlobs(prefix: path).AsPages())
            {
                DateTime checkFileModified = (page.Values.ToList().OrderByDescending(m => m.Properties.LastModified).ToList().First()).Properties.LastModified.Value.LocalDateTime;
                lastUpdated = prevUpdated = prevUpdated < checkFileModified ? checkFileModified : prevUpdated;
            }
            return lastUpdated;
        }

        // Converts the byte size value to appropriate value
        protected string ByteConversion(long fileSize)
        {
            try
            {
                string[] index = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
                // Longs run out around EB
                if (fileSize == 0)
                {
                    return "0 " + index[0];
                }
                int value = Convert.ToInt32(Math.Floor(Math.Log(Math.Abs(fileSize), 1024)));
                return (Math.Sign(fileSize) * Math.Round(Math.Abs(fileSize) / Math.Pow(1024, value), 1)).ToString() + " " + index[value];
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Gets the size value of the directory
        protected async Task<long> GetSizeValue(string path)
        {
            foreach (Azure.Page<BlobItem> page in container.GetBlobs(prefix: path + "/").AsPages())
            {
                foreach (BlobItem item in page.Values)
                {
                    BlobClient blob = container.GetBlobClient(item.Name);
                    BlobProperties properties = await blob.GetPropertiesAsync();
                    size += properties.ContentLength;
                }
            }
            return size;
        }

        // Gets details of the files
        public FileManagerResponse Details(string path, string[] names, params FileManagerDirectoryContent[] data)
        {
            return GetDetailsAsync(path, names, data).GetAwaiter().GetResult();
        }

        // Gets the details
        protected async Task<FileManagerResponse> GetDetailsAsync(string path, string[] names, IEnumerable<object> selectedItems = null)
        {
            bool isVariousFolders = false;
            string previousPath = "";
            string previousName = "";
            FileManagerResponse detailsResponse = new FileManagerResponse();
            try
            {
                bool isFile = false;
                bool namesAvailable = names.Length > 0;
                if (names.Length == 0 && selectedItems != null && selectedItems.Count() > 0)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        bool Status = selectedItems.Any();
                        if (Status)
                        {
                            List<string> values = new List<string>();
                            foreach (FileManagerDirectoryContent item in selectedItems)
                            {
                                values.Add(item.Name);
                            }
                            names = values.ToArray();
                        }
                    }
                }
                FileDetails fileDetails = new FileDetails();
                long multipleSize = 0;
                if (selectedItems != null && selectedItems.Count()>0)
                {
                    foreach (FileManagerDirectoryContent fileItem in selectedItems)
                    {
                        if (names.Length == 1)
                        {
                            if (fileItem.IsFile)
                            {
                                if (fileItem.FilterPath == "/" || string.IsNullOrEmpty(path))
                                {
                                    fileDirectory = await AzureShareConfigSettingsAsync();
                                    CloudFile Sourcefile = fileDirectory.GetFileReference(fileItem.Name);
                                    //get the cloudfile's propertities and metadata 
                                    await Sourcefile.FetchAttributesAsync();
                                    isFile = fileItem.IsFile;
                                    fileDetails.IsFile = isFile;
                                    fileDetails.Name = Sourcefile.Name;
                                    fileDetails.Location = Sourcefile.Uri.ToString();
                                    DateTimeOffset? myDTO = new DateTimeOffset();
                                    myDTO = Sourcefile.Properties.LastModified;
                                    DateTime utc = myDTO.Value.UtcDateTime;
                                    fileDetails.Size = ByteConversion(Sourcefile.Properties.Length); fileDetails.Modified = utc; detailsResponse.Details = fileDetails;

                                }
                                else
                                {
                                    fileDirectory = await AzureShareConfigSettingsAsync();
                                    string CurrentPath = fileItem.FilterPath.Remove(0, 1);
                                    var directory = fileDirectory.GetDirectoryReference(CurrentPath);
                                    CloudFile Sourcefile = directory.GetFileReference(fileItem.Name);
                                    //get the cloudfile's propertities and metadata 
                                    await Sourcefile.FetchAttributesAsync();
                                    isFile = fileItem.IsFile;
                                    fileDetails.IsFile = isFile;
                                    fileDetails.Name = Sourcefile.Name;
                                    fileDetails.Location = Sourcefile.Uri.ToString();
                                    DateTimeOffset? myDTO = new DateTimeOffset();
                                    myDTO = Sourcefile.Properties.LastModified;
                                    DateTime utc = myDTO.Value.UtcDateTime;
                                    fileDetails.Size = ByteConversion(Sourcefile.Properties.Length);
                                    fileDetails.Modified = utc; detailsResponse.Details = fileDetails;
                                }
                            }
                            else
                            {
                                long sizeValue = GetSizeValue((namesAvailable ? rootPath + fileItem.FilterPath + fileItem.Name : path.TrimEnd('/'))).Result;
                                isFile = false;
                                fileDetails.Name = fileItem.Name;
                                fileDetails.Location = (namesAvailable ? rootPath + fileItem.FilterPath + fileItem.Name : path.Substring(0, path.Length - 1)).Replace("/", @"\");
                                fileDetails.Size = ByteConversion(sizeValue); fileDetails.Modified = await DirectoryLastModified(path); detailsResponse.Details = fileDetails;
                            }
                        }
                        else
                        {
                            multipleSize += (fileItem.IsFile ? fileItem.Size : GetSizeValue(namesAvailable ? rootPath + fileItem.FilterPath + fileItem.Name : path).Result);
                            size = 0;
                            fileDetails.Name = previousName == "" ? previousName = fileItem.Name : previousName + ", " + fileItem.Name;
                            previousPath = previousPath == "" ? rootPath + fileItem.FilterPath : previousPath;
                            if (previousPath == rootPath + fileItem.FilterPath && !isVariousFolders)
                            {
                                previousPath = rootPath + fileItem.FilterPath;
                                fileDetails.Location = ((rootPath + fileItem.FilterPath).Replace("/", @"\")).Substring(0, (rootPath + fileItem.FilterPath).Replace(" / ", @"\").Length - 1);
                            }
                            else
                            {
                                isVariousFolders = true;
                                fileDetails.Location = "Various Folders";
                            }
                            fileDetails.Size = ByteConversion(multipleSize); fileDetails.MultipleFiles = true; detailsResponse.Details = fileDetails;
                        }
                    }
                }
                return await Task.Run(() =>
                {
                    size = 0;
                    return detailsResponse;
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Creates a new folder
        public async Task<FileManagerResponse> CreateAsync(string path, string name,
            params FileManagerDirectoryContent[] selectedItems)
        {
            FileManagerResponse createResponse = new FileManagerResponse();
            string finalPath = string.Empty;
            this.isFolderAvailable = false;
            fileDirectory = await AzureShareConfigSettingsAsync();
            if (path == "/" || string.IsNullOrEmpty(path))
            {
                var folder = fileDirectory.GetDirectoryReference(name);
                if (await folder.ExistsAsync())
                {
                    ErrorDetails error = new ErrorDetails();
                    error.FileExists = existFiles;
                    error.Code = "400";
                    error.Message = "Folder Already Exists";
                    createResponse.Error = error;
                    return createResponse;
                }
                bool Status = await folder.CreateIfNotExistsAsync();
                if (Status == true)
                {
                    // Folder Created
                }
                else
                {
                    ErrorDetails error = new ErrorDetails();
                    error.FileExists = existFiles;
                    error.Code = "400";
                    error.Message = "Folder Already Exists";
                    createResponse.Error = error;
                    return createResponse;
                }

            }
            else
            {
                string newpath = selectedItems[0].FilterPath.Remove(0, 1);
                finalPath= newpath + selectedItems[0].Name+ "/";
                var folder = fileDirectory.GetDirectoryReference(finalPath + name);
                await folder.CreateIfNotExistsAsync();
            }
            FileManagerDirectoryContent content = new FileManagerDirectoryContent();
            content.Name = name;
            content.FilterPath = finalPath + name;
            FileManagerDirectoryContent[] directories = new[] { content };
            createResponse.Files = (IEnumerable<FileManagerDirectoryContent>)directories;
            return createResponse;
        }

        // Creates a new folder
        protected async Task CreateFolderAsync(string path, string name, IEnumerable<object> selectedItems = null)
        {
            string checkName = name.Contains(" ") ? name.Replace(" ", "%20") : name;
            foreach (Azure.Page<BlobHierarchyItem> page in container.GetBlobsByHierarchy(prefix: path, delimiter: "/").AsPages())
            {
                List<BlobItem> items = page.Values.Where(item => item.IsBlob).Select(item => item.Blob).ToList();
                if (await IsFolderExists(path + name) || (items.Where(x => x.Name.Split("/").Last().Replace("/", "").ToLower() == checkName.ToLower()).Select(i => i).ToArray().Length > 0))
                {
                    this.isFolderAvailable = true;
                }
                else
                {
                    BlobClient blob = container.GetBlobClient(path + name + "/About.txt");
                    await blob.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes("This is a auto generated file")), new BlobHttpHeaders() { ContentType = "text/plain" });
                }
            }
        }

        // Renames file(s) or folder(s)
        public FileManagerResponse Rename(string path, string oldName, string newName, bool replace = false, params FileManagerDirectoryContent[] data)
        {
            return RenameAsync(path, oldName, newName, data).GetAwaiter().GetResult();
        }

        // Renames file(s) or folder(s)
        protected async Task<FileManagerResponse> RenameAsync(string path, string oldName, string newName, params FileManagerDirectoryContent[] selectedItems)
        {
            FileManagerResponse renameResponse = new FileManagerResponse();
            List<FileManagerDirectoryContent> details = new List<FileManagerDirectoryContent>();
            FileManagerDirectoryContent entry = new FileManagerDirectoryContent();
            bool isAlreadyAvailable = false;
            bool isFile = false;
            foreach (FileManagerDirectoryContent fileItem in selectedItems)
            {
                FileManagerDirectoryContent directoryContent = fileItem;
                isFile = directoryContent.IsFile;
                isAlreadyAvailable = isFile ? await IsFileExists(path + newName) : await IsFolderExists(path + newName);
                entry.Name = newName;
                entry.Type = directoryContent.Type;
                entry.IsFile = isFile;
                entry.Size = directoryContent.Size;
                entry.HasChild = directoryContent.HasChild;
                entry.FilterPath = path;
                details.Add(entry);
                break;
            }
            if (!isAlreadyAvailable)
            {
                if (isFile)
                {
                    BlobClient existBlob = container.GetBlobClient(path + oldName);
                    await (container.GetBlobClient(path + newName)).StartCopyFromUriAsync(existBlob.Uri);
                    await existBlob.DeleteAsync();
                }
                else
                {
                    foreach (Azure.Page<BlobItem> page in container.GetBlobs(prefix: path + oldName + "/").AsPages())
                    {
                        foreach (BlobItem item in page.Values)
                        {
                            string name = container.GetBlobClient(item.Name).Uri.AbsolutePath.Replace(container.GetBlobClient(path + oldName).Uri.AbsolutePath + "/", "").Replace("%20", " ");
                            await (container.GetBlobClient(path + newName + "/" + name)).StartCopyFromUriAsync(container.GetBlobClient(item.Name).Uri);
                            await container.GetBlobClient(path + oldName + "/" + name).DeleteAsync();
                        }
                    }
                }
                renameResponse.Files = details;
            }
            else
            {
                ErrorDetails error = new ErrorDetails();
                error.FileExists = existFiles;
                error.Code = "400";
                error.Message = "File or Folder Already Exists";
                renameResponse.Error = error;
            }
            return renameResponse;
        }

        public async Task<FileManagerResponse> DeleteAzureSMBFilesAsync(string path, string[] names,
            params FileManagerDirectoryContent[] selectedItems)
        {
            fileDirectory = await AzureShareConfigSettingsAsync();
            return Delete(path, names, selectedItems);
        }

        // Deletes file(s) or folder(s)
        public FileManagerResponse Delete(string path, string[] names, params FileManagerDirectoryContent[] data)
        {
            return RemoveAsync(names, path, data).GetAwaiter().GetResult();
        }

        public async Task deletedir(CloudFileDirectory dir)
        {
            FileContinuationToken? token = null;
            FileResultSegment resultSegment = await dir.ListFilesAndDirectoriesSegmentedAsync(token);
            List<IListFileItem> results = new List<IListFileItem>();
            results.AddRange(resultSegment.Results);
            if (resultSegment.Results.Count() == 0)
            {
                if (await dir.ExistsAsync())
                    await dir.DeleteAsync();
            }
            else
            {
                foreach (IListFileItem listItem in results)
                {
                    if (listItem.GetType() == typeof(CloudFile))
                    {
                        CloudFile file = (CloudFile)listItem;
                        await file.DeleteIfExistsAsync();
                        // Do whatever
                    }
                    else if (listItem.GetType() == typeof(CloudFileDirectory))
                    {
                        CloudFileDirectory dir1 = (CloudFileDirectory)listItem;
                        FileResultSegment resultSegment1 = await dir1.ListFilesAndDirectoriesSegmentedAsync(token);
                        if (resultSegment1.Results.Count() == 0)
                        {
                            if (await dir1.ExistsAsync())
                                await dir1.DeleteAsync();
                        }
                        else
                        {
                            deletedir(dir1);
                        }
                    }
                }
            }
        }

        // Deletes file(s) or folder(s)
        protected async Task<FileManagerResponse> RemoveAsync(string[] names, string path, params FileManagerDirectoryContent[] selectedItems)
        {
            try
            {
                FileManagerResponse removeResponse = new FileManagerResponse();
                List<FileManagerDirectoryContent> details = new List<FileManagerDirectoryContent>();
                FileManagerDirectoryContent entry = new FileManagerDirectoryContent();
                fileDirectory = await AzureShareConfigSettingsAsync();
                CloudFileDirectory rootDir = fileShare.GetRootDirectoryReference();
                foreach (FileManagerDirectoryContent fileItem in selectedItems)
                {
                    if (fileItem.IsFile)
                    {
                        if (path != "/")
                        {
                            string newpath = string.Empty;
                            if (path.Contains("/") == true)
                            {
                                newpath = path.Remove(0, 1);
                            }
                            else
                            {
                                newpath = path;
                            }
                            var directory = rootDir.GetDirectoryReference(newpath);
                            CloudFile Sourcefile = directory.GetFileReference(fileItem.Name);
                            if (await Sourcefile.ExistsAsync())
                            {
                                await Sourcefile.DeleteAsync();
                            }
                        }
                        else
                        {
                            string newpath = string.Empty;
                            if (path.Contains("/") == true)
                            {
                                newpath = path;
                            }
                            else
                            {
                                newpath = path;
                            }
                            var directory = rootDir.GetFileReference(fileItem.Name);
                            try
                            {
                                if (await directory.ExistsAsync())
                                {
                                    await directory.DeleteAsync();
                                };
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }
                        }
                    }
                    else
                    {
                        if (path != "/")
                        {
                            string newpath = string.Empty;
                            if (path.Contains("/") == true)
                            {
                                newpath = path.Remove(0, 1);
                            }
                            else
                            {
                                newpath = path;
                            }
                            var directory = rootDir.GetDirectoryReference(newpath);
                            CloudFileDirectory? SourceFolder = directory.GetDirectoryReference(fileItem.Name);
                            if (await SourceFolder.ExistsAsync())
                            {
                                List<IListFileItem> results = new List<IListFileItem>();
                                FileContinuationToken? token = null;
                                do
                                {
                                    FileResultSegment resultSegment = SourceFolder.ListFilesAndDirectoriesSegmentedAsync
                                        (token).GetAwaiter().GetResult();
                                    if (Path.HasExtension(fileItem.Name))
                                    {
                                        results.Add(SourceFolder.GetFileReference(fileItem.Name));
                                    }
                                    else
                                    {
                                        results.AddRange(resultSegment.Results);
                                    }
                                    token = resultSegment.ContinuationToken;
                                    if (resultSegment.Results.Count() == 0)
                                    {
                                        if (await SourceFolder.ExistsAsync())
                                            await SourceFolder.DeleteIfExistsAsync();
                                    }
                                }
                                while (token != null);
                                foreach (IListFileItem listItem in results)
                                {
                                    if (listItem.GetType() == typeof(CloudFile))
                                    {
                                        CloudFile file = (CloudFile)listItem;
                                        await file.DeleteIfExistsAsync();
                                        // Do whatever
                                    }
                                    else if (listItem.GetType() == typeof(CloudFileDirectory))
                                    {
                                        CloudFileDirectory dir = (CloudFileDirectory)listItem;
                                        await dir.DeleteIfExistsAsync();
                                        // Do whatever
                                    }
                                }
                                if (!Path.HasExtension(fileItem.Name))
                                {
                                    //await fileDirectory.DeleteIfExistsAsync();
                                }
                            }
                        }
                        else
                        {
                            string newpath = string.Empty;
                            if (path.Contains("/") == true)
                            {
                                newpath = path;
                            }
                            else
                            {
                                newpath = path;
                            }
                            CloudFileDirectory? SourceFolder = rootDir.GetDirectoryReference(fileItem.Name);
                            //CloudFileDirectory CD = SourceFolder.GetDirectoryReference(path);
                            if (await SourceFolder.ExistsAsync())
                            {
                                List<IListFileItem> results = new List<IListFileItem>();
                                FileContinuationToken? token = null;
                                do
                                {
                                    FileResultSegment resultSegment = SourceFolder.ListFilesAndDirectoriesSegmentedAsync
                                        (token).GetAwaiter().GetResult();
                                    if (Path.HasExtension(fileItem.Name))
                                    {
                                        results.Add(SourceFolder.GetFileReference(fileItem.Name));
                                    }
                                    else
                                    {
                                        results.AddRange(resultSegment.Results);
                                    }
                                    token = resultSegment.ContinuationToken;
                                    if (resultSegment.Results.Count() == 0)
                                    {
                                        if (await SourceFolder.ExistsAsync())
                                            await SourceFolder.DeleteIfExistsAsync();
                                    }
                                }
                                while (token != null);
                                foreach (IListFileItem listItem in results)
                                {
                                    if (listItem.GetType() == typeof(CloudFile))
                                    {
                                        CloudFile file = (CloudFile)listItem;
                                        await file.DeleteIfExistsAsync();
                                        // Do whatever
                                    }
                                    else if (listItem.GetType() == typeof(CloudFileDirectory))
                                    {
                                        CloudFileDirectory dir = (CloudFileDirectory)listItem;
                                        await deletedir(dir);
                                        // Do whatever
                                    }
                                }
                                if (!Path.HasExtension(fileItem.Name))
                                {
                                   // await SourceFolder.DeleteIfExistsAsync();
                                }
                            }
                        }
                    }
                }
                removeResponse.Files = details;
                return removeResponse;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // Upload file(s) to the storage
        public FileManagerResponse Upload(string path, IList<IFormFile> files, string action, params FileManagerDirectoryContent[] data)
        {
            return UploadAsync(files, action, path, data).GetAwaiter().GetResult();
        }

        // Upload file(s) to the storage
        protected async Task<FileManagerResponse> UploadAsync(IEnumerable<IFormFile> files, string action, string path, IEnumerable<object> selectedItems = null)
        {
            FileManagerResponse uploadResponse = new FileManagerResponse();
            try
            {
                foreach (IFormFile file in files)
                {
                    if (files != null)
                    {
                        BlobClient blockBlob = container.GetBlobClient(path.Replace(blobPath, "") + file.FileName);
                        string fileName = file.FileName;
                        string absoluteFilePath = Path.Combine(path, fileName);
                        if (action == "save")
                        {
                            if (!await IsFileExists(absoluteFilePath))
                            {
                                await blockBlob.UploadAsync(file.OpenReadStream());
                            }
                            else
                            {
                                existFiles.Add(fileName);
                            }
                        }
                        else if (action == "replace")
                        {
                            if (await IsFileExists(absoluteFilePath))
                            {
                                await blockBlob.DeleteAsync();
                            }
                            await blockBlob.UploadAsync(file.OpenReadStream());
                        }
                        else if (action == "keepboth")
                        {
                            string newAbsoluteFilePath = absoluteFilePath;
                            string newFileName = file.FileName;
                            int index = absoluteFilePath.LastIndexOf(".");
                            int indexValue = newFileName.LastIndexOf(".");
                            if (index >= 0)
                            {
                                newAbsoluteFilePath = absoluteFilePath.Substring(0, index);
                                newFileName = newFileName.Substring(0, indexValue);
                            }
                            int fileCount = 0;
                            while (await IsFileExists(newAbsoluteFilePath + (fileCount > 0 ? "(" + fileCount.ToString() + ")" + Path.GetExtension(fileName) : Path.GetExtension(fileName))))
                            {
                                fileCount++;
                            }
                            newAbsoluteFilePath = newFileName + (fileCount > 0 ? "(" + fileCount.ToString() + ")" : "") + Path.GetExtension(fileName);
                            BlobClient newBlob = container.GetBlobClient(path.Replace(blobPath, "") + newAbsoluteFilePath);
                            await newBlob.UploadAsync(file.OpenReadStream());
                        }
                    }
                }
                if (existFiles.Count != 0)
                {
                    ErrorDetails error = new ErrorDetails();
                    error.FileExists = existFiles;
                    error.Code = "400";
                    error.Message = "File Already Exists";
                    uploadResponse.Error = error;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return uploadResponse;
        }

        protected async Task CopyFileToTemp(string path, BlobClient blockBlob)
        {
            using (FileStream fileStream = File.Create(path))
            {
                await blockBlob.DownloadToAsync(fileStream);
                fileStream.Close();
            }
        }

        // Download file(s) from the storage
        public async Task<DownloadFile_VM> DownloadAsync(string path, string[] names = null, params FileManagerDirectoryContent[] selectedItems)
        {
            return await AzureFileDownloadAsync(filesPath + path + "", names, selectedItems);
        }

        // Download file(s) from the storage
        protected async Task<DownloadFile_VM> AzureFileDownloadAsync(string path, string[] names = null, params FileManagerDirectoryContent[] selectedItems)
        {
            var memoryStream = new MemoryStream();
            DownloadFile_VM obj = new DownloadFile_VM();
            foreach (FileManagerDirectoryContent file in selectedItems)
            {
                if (file.IsFile)
                {
                    string Path = file.FilterPath;
                    string FileName = file.Name;
                    fileDirectory = await AzureShareConfigSettingsAsync();
                    CloudFileDirectory rootDir = fileShare.GetRootDirectoryReference();
                    if (path != "/")
                    {
                        string newpath = string.Empty;
                        if (path.Contains("/") == true)
                        {
                            newpath = path.Remove(0, 1);

                        }
                        else
                        {
                            newpath = path;
                        }
                        var directory = rootDir.GetDirectoryReference(newpath);
                        CloudFile Sourcefile = directory.GetFileReference(FileName);
                        if (await Sourcefile.ExistsAsync())
                        {

                            await Sourcefile.DownloadToStreamAsync(memoryStream);
                            obj.stream = memoryStream;
                            obj.fileName = FileName;
                            return obj;
                        }
                    }
                    else
                    {
                        CloudFile Sourcefile = fileDirectory.GetFileReference(FileName);
                        if (await Sourcefile.ExistsAsync())
                        {

                            await Sourcefile.DownloadToStreamAsync(memoryStream);
                            obj.stream = memoryStream;
                            obj.fileName = FileName;
                            return obj;
                        }
                    }
                }
            }
            return null;
        }

        public async Task<FileStreamResult> GetImage(string path, string id, bool allowCompress, ImageSize size, params FileManagerDirectoryContent[] data)
        {
            fileDirectory = await AzureShareConfigSettingsAsync();
            CloudFileDirectory rootDir = fileShare.GetRootDirectoryReference();
            var memoryStream = new MemoryStream();
            string Path= path.Remove(0, 1);
            CloudFile Sourcefile = fileDirectory.GetFileReference(Path);
            if (await Sourcefile.ExistsAsync())
            {

                await Sourcefile.DownloadToStreamAsync(memoryStream);
            }
            return new FileStreamResult(memoryStream, "APPLICATION/octet-stream");
        }

        // Download folder(s) from the storage
        private async Task DownloadFolder(string path, string folderName, ZipArchiveEntry zipEntry, ZipArchive archive)
        {
            zipEntry = archive.CreateEntry(currentFolderName + "/");
            foreach (Azure.Page<BlobHierarchyItem> page in container.GetBlobsByHierarchy(prefix: pathValue, delimiter: "/").AsPages())
            {
                foreach (BlobItem item in page.Values.Where(item => item.IsBlob).Select(item => item.Blob))
                {
                    BlobClient blob = container.GetBlobClient(item.Name);
                    int index = blob.Name.LastIndexOf("/");
                    string fileName = blob.Name.Substring(index + 1);
                    string absoluteFilePath = Path.GetTempPath() + blob.Name.Split("/").Last();
                    if (File.Exists(absoluteFilePath))
                    {
                        File.Delete(absoluteFilePath);
                    }
                    await CopyFileToTemp(absoluteFilePath, blob);
                    zipEntry = archive.CreateEntryFromFile(absoluteFilePath, currentFolderName + "\\" + fileName, CompressionLevel.Fastest);
                    if (File.Exists(absoluteFilePath))
                    {
                        File.Delete(absoluteFilePath);
                    }
                }
                foreach (string item in page.Values.Where(item => item.IsPrefix).Select(item => item.Prefix))
                {
                    string absoluteFilePath = item.Replace(blobPath, ""); // <-- Change your download target path here
                    pathValue = absoluteFilePath;
                    string targetPath = item.Replace(filesPath + "/", "");
                    string folderPath = new DirectoryInfo(targetPath).Name;
                    currentFolderName = previousFolderName.Length > 1 ? item.Replace(previousFolderName, "").Trim('/') : item.Trim('/');
                    await DownloadFolder(absoluteFilePath, folderPath, zipEntry, archive);
                }
            }
        }

        // Check whether the directory has child
        private async Task<bool> HasChildDirectory(string path)
        {
            List<string> prefixes = new List<string>() { };
            foreach (Azure.Page<BlobHierarchyItem> page in container.GetBlobsByHierarchy(prefix: path, delimiter: "/").AsPages())
            {
                prefixes = page.Values.Where(item => item.IsPrefix).Select(item => item.Prefix).ToList();
            }
            return prefixes.Count != 0;
        }

        // To get the file details
        private static FileManagerDirectoryContent GetFileDetails(string targetPath, FileManagerDirectoryContent fileDetails)
        {
            FileManagerDirectoryContent entry = new FileManagerDirectoryContent();
            entry.Name = fileDetails.Name;
            entry.Type = fileDetails.Type;
            entry.IsFile = fileDetails.IsFile;
            entry.Size = fileDetails.Size;
            entry.HasChild = fileDetails.HasChild;
            entry.FilterPath = targetPath;
            return entry;
        }

        // To check if folder exists
        private async Task<bool> IsFolderExists(string path)
        {
            List<string> x = new List<string>() { };
            foreach (Azure.Page<BlobHierarchyItem> page in container.GetBlobsByHierarchy(prefix: path, delimiter: "/").AsPages())
            {
                x = page.Values.Where(item => item.IsPrefix).Select(item => item.Prefix).ToList();
            }
            return x.Count > 0;
        }

        // To check if file exists
        private async Task<bool> IsFileExists(string path)
        {
            BlobClient newBlob = container.GetBlobClient(path);
            return await newBlob.ExistsAsync();
        }

        // Copies file(s) or folders
        public FileManagerResponse Copy(string path, string targetPath, string[] names, string[] renameFiles, FileManagerDirectoryContent targetData, params FileManagerDirectoryContent[] data)
        {
            return CopyToAsync(path, targetPath, names, renameFiles, data).GetAwaiter().GetResult();
        }

        private async Task<FileManagerResponse> CopyToAsync(string path, string targetPath, string[] names, string[] renamedFiles = null, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse copyResponse = new FileManagerResponse();
            try
            {
                renamedFiles = renamedFiles ?? Array.Empty<string>();
                foreach (FileManagerDirectoryContent item in data)
                {
                    if (item.IsFile)
                    {
                        if (await IsFileExists(targetPath + item.Name))
                        {
                            int index = -1;
                            if (renamedFiles.Length > 0)
                            {
                                index = Array.FindIndex(renamedFiles, Items => Items.Contains(item.Name));
                            }
                            if ((path == targetPath) || (index != -1))
                            {
                                string newName = await FileRename(targetPath, item.Name);
                                CopyItems(rootPath + item.FilterPath, targetPath, item.Name, newName);
                                copiedFiles.Add(GetFileDetails(targetPath, item));
                            }
                            else
                            {
                                this.existFiles.Add(item.Name);
                            }
                        }
                        else
                        {
                            CopyItems(rootPath + item.FilterPath, targetPath, item.Name, null);
                            copiedFiles.Add(GetFileDetails(targetPath, item));
                        }
                    }
                    else
                    {
                        if (!await IsFolderExists((rootPath + item.FilterPath + item.Name)))
                        {
                            missingFiles.Add(item.Name);
                        }
                        else if (await IsFolderExists(targetPath + item.Name))
                        {
                            int index = -1;
                            if (renamedFiles.Length > 0)
                            {
                                index = Array.FindIndex(renamedFiles, Items => Items.Contains(item.Name));
                            }
                            if ((path == targetPath) || (index != -1))
                            {
                                item.Path = rootPath + item.FilterPath + item.Name;
                                item.Name = await FileRename(targetPath, item.Name);
                                CopySubFolder(item, targetPath);
                                copiedFiles.Add(GetFileDetails(targetPath, item));
                            }
                            else
                            {
                                existFiles.Add(item.Name);
                            }
                        }
                        else
                        {
                            item.Path = rootPath + item.FilterPath + item.Name;
                            CopySubFolder(item, targetPath);
                            copiedFiles.Add(GetFileDetails(targetPath, item));
                        }
                    }

                }
                copyResponse.Files = copiedFiles;
                if (existFiles.Count > 0)
                {
                    ErrorDetails error = new ErrorDetails();
                    error.FileExists = existFiles;
                    error.Code = "400";
                    error.Message = "File Already Exists";
                    copyResponse.Error = error;
                }
                if (missingFiles.Count > 0)
                {
                    string missingFilesList = missingFiles[0];
                    for (int k = 1; k < missingFiles.Count; k++)
                    {
                        missingFilesList = missingFilesList + ", " + missingFiles[k];
                    }
                    throw new FileNotFoundException(missingFilesList + " not found in given location.");
                }
                return copyResponse;
            }
            catch (Exception e)
            {
                ErrorDetails error = new ErrorDetails();
                error.Code = "404";
                error.Message = e.Message.ToString();
                error.FileExists = copyResponse.Error?.FileExists;
                copyResponse.Error = error;
                return copyResponse;
            }
        }

        // To iterate and copy subfolder
        private void CopySubFolder(FileManagerDirectoryContent subFolder, string targetPath)
        {
            targetPath = targetPath + subFolder.Name + "/";
            foreach (Azure.Page<BlobHierarchyItem> page in container.GetBlobsByHierarchy(prefix: subFolder.Path + "/", delimiter: "/").AsPages())
            {
                foreach (BlobItem item in page.Values.Where(item => item.IsBlob).Select(item => item.Blob))
                {
                    string name = item.Name.Replace(subFolder.Path + "/", "");
                    string sourcePath = item.Name.Replace(name, "");
                    CopyItems(sourcePath, targetPath, name, null);
                }
                foreach (string item in page.Values.Where(item => item.IsPrefix).Select(item => item.Prefix))
                {
                    FileManagerDirectoryContent itemDetail = new FileManagerDirectoryContent();
                    itemDetail.Name = item.Replace(subFolder.Path, "").Replace("/", "");
                    itemDetail.Path = subFolder.Path + "/" + itemDetail.Name;
                    CopySubFolder(itemDetail, targetPath);
                }
            }
        }

        // To iterate and copy files
        private void CopyItems(string sourcePath, string targetPath, string name, string newName)
        {
            if (newName == null)
            {
                newName = name;
            }
            BlobClient existBlob = container.GetBlobClient(sourcePath + name);
            BlobClient newBlob = container.GetBlobClient(targetPath + newName);
            newBlob.StartCopyFromUri(existBlob.Uri);
        }

        // To rename files incase of duplicates
        private async Task<string> FileRename(string newPath, string fileName)
        {
            int index = fileName.LastIndexOf(".");
            string nameNotExist = string.Empty;
            nameNotExist = index >= 0 ? fileName.Substring(0, index) : fileName;
            int fileCount = 0;
            while (index > -1 ? await IsFileExists(newPath + nameNotExist + (fileCount > 0 ? "(" + fileCount.ToString() + ")" + Path.GetExtension(fileName) : Path.GetExtension(fileName))) : await IsFolderExists(newPath + nameNotExist + (fileCount > 0 ? "(" + fileCount.ToString() + ")" + Path.GetExtension(fileName) : Path.GetExtension(fileName))))
            {
                fileCount++;
            }
            fileName = nameNotExist + (fileCount > 0 ? "(" + fileCount.ToString() + ")" : "") + Path.GetExtension(fileName);
            return await Task.Run(() =>
            {
                return fileName;
            });
        }

        // Returns the image 
        

        private async Task MoveItems(string sourcePath, string targetPath, string name, string newName)
        {
            BlobClient existBlob = container.GetBlobClient(sourcePath + name);
            CopyItems(sourcePath, targetPath, name, newName);
            await existBlob.DeleteAsync();
        }

        private async void MoveSubFolder(FileManagerDirectoryContent subFolder, string targetPath)
        {
            targetPath = targetPath + subFolder.Name + "/";
            foreach (Azure.Page<BlobHierarchyItem> page in container.GetBlobsByHierarchy(prefix: subFolder.Path + "/", delimiter: "/").AsPages())
            {
                foreach (BlobItem item in page.Values.Where(item => item.IsBlob).Select(item => item.Blob))
                {
                    string name = item.Name.Replace(subFolder.Path + "/", "");
                    string sourcePath = item.Name.Replace(name, "");
                    await MoveItems(sourcePath, targetPath, name, null);
                }
                foreach (string item in page.Values.Where(item => item.IsPrefix).Select(item => item.Prefix))
                {
                    FileManagerDirectoryContent itemDetail = new FileManagerDirectoryContent();
                    itemDetail.Name = item.Replace(subFolder.Path, "").Replace("/", "");
                    itemDetail.Path = subFolder.Path + "/" + itemDetail.Name;
                    CopySubFolder(itemDetail, targetPath);
                }
            }
        }

        // Moves file(s) or folders
        public FileManagerResponse Move(string path, string targetPath, string[] names, string[] renameFiles, FileManagerDirectoryContent targetData, params FileManagerDirectoryContent[] data)
        {
            return MoveToAsync(path, targetPath, names, renameFiles, data).GetAwaiter().GetResult();
        }

        private async Task<FileManagerResponse> MoveToAsync(string path, string targetPath, string[] names, string[] renamedFiles = null, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse moveResponse = new FileManagerResponse();
            try
            {
                renamedFiles = renamedFiles ?? Array.Empty<string>();
                foreach (FileManagerDirectoryContent item in data)
                {
                    if (item.IsFile)
                    {
                        if (await IsFileExists(targetPath + item.Name))
                        {
                            int index = -1;
                            if (renamedFiles.Length > 0)
                            {
                                index = Array.FindIndex(renamedFiles, Items => Items.Contains(item.Name));
                            }
                            if ((path == targetPath) || (index != -1))
                            {
                                string newName = await FileRename(targetPath, item.Name);
                                await MoveItems(rootPath + item.FilterPath, targetPath, item.Name, newName);
                                copiedFiles.Add(GetFileDetails(targetPath, item));
                            }
                            else
                            {
                                this.existFiles.Add(item.Name);
                            }
                        }
                        else
                        {
                            await MoveItems(rootPath + item.FilterPath, targetPath, item.Name, null);
                            copiedFiles.Add(GetFileDetails(targetPath, item));
                        }
                    }
                    else
                    {
                        if (!await IsFolderExists(rootPath + item.FilterPath + item.Name))
                        {
                            missingFiles.Add(item.Name);
                        }
                        else if (await IsFolderExists(targetPath + item.Name))
                        {
                            int index = -1;
                            if (renamedFiles.Length > 0)
                            {
                                index = Array.FindIndex(renamedFiles, Items => Items.Contains(item.Name));
                            }
                            if ((path == targetPath) || (index != -1))
                            {
                                item.Path = rootPath + item.FilterPath + item.Name;
                                item.Name = await FileRename(targetPath, item.Name);
                                MoveSubFolder(item, targetPath);
                                copiedFiles.Add(GetFileDetails(targetPath, item));
                            }
                            else
                            {
                                existFiles.Add(item.Name);
                            }
                        }
                        else
                        {
                            item.Path = rootPath + item.FilterPath + item.Name;
                            MoveSubFolder(item, targetPath);
                            copiedFiles.Add(GetFileDetails(targetPath, item));
                        }
                    }
                }
                moveResponse.Files = copiedFiles;
                if (existFiles.Count > 0)
                {
                    ErrorDetails error = new ErrorDetails();
                    error.FileExists = existFiles;
                    error.Code = "400";
                    error.Message = "File Already Exists";
                    moveResponse.Error = error;
                }
                if (missingFiles.Count > 0)
                {
                    string nameList = missingFiles[0];
                    for (int k = 1; k < missingFiles.Count; k++)
                    {
                        nameList = nameList + ", " + missingFiles[k];
                    }
                    throw new FileNotFoundException(nameList + " not found in given location.");
                }
                return moveResponse;
            }
            catch (Exception e)
            {
                ErrorDetails error = new ErrorDetails();
                error.Code = "404";
                error.Message = e.Message.ToString();
                error.FileExists = moveResponse.Error?.FileExists;
                moveResponse.Error = error;
                return moveResponse;
            }
        }

        // Search for file(s) or folders
        public FileManagerResponse Search(string path, string searchString, bool showHiddenItems, bool caseSensitive, params FileManagerDirectoryContent[] data)
        {
            directoryContentItems.Clear();
            FileManagerResponse searchResponse = null;//GetFiles(path);
            directoryContentItems.AddRange(searchResponse.Files);
            GetAllFiles(path, searchResponse);
            searchResponse.Files = directoryContentItems.Where(item => new Regex(WildcardToRegex(searchString), (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase)).IsMatch(item.Name));
            return searchResponse;
        }

        // Gets all files
        protected virtual void GetAllFiles(string path, FileManagerResponse data)
        {
            FileManagerResponse directoryList = new FileManagerResponse();
            directoryList.Files = data.Files.Where(item => item.IsFile == false);
            for (int i = 0; i < directoryList.Files.Count(); i++)
            {
                FileManagerResponse innerData = null; //GetFiles(path + directoryList.Files.ElementAt(i).Name + "/", true, (new[] { directoryList.Files.ElementAt(i) }));
                innerData.Files = innerData.Files.Select(file => new FileManagerDirectoryContent
                {
                    Name = file.Name,
                    Type = file.Type,
                    IsFile = file.IsFile,
                    Size = file.Size,
                    HasChild = file.HasChild,
                    FilterPath = (file.FilterPath)
                });
                directoryContentItems.AddRange(innerData.Files);
                GetAllFiles(path + directoryList.Files.ElementAt(i).Name + "/", innerData);
            }
        }

        protected virtual string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        }
    }
}
