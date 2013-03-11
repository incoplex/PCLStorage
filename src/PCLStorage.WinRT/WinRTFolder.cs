﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PCLStorage
{
    [DebuggerDisplay("Name = {Name}")]
	public class WinRTFolder : IFolder
	{
        private readonly IStorageFolder _wrappedFolder;
        private readonly bool _isRootFolder;

        private WinRTFolder(IStorageFolder wrappedFolder, bool isRootFolder)
		{
			_wrappedFolder = wrappedFolder;
            _isRootFolder = isRootFolder;
		}

        public WinRTFolder(IStorageFolder wrappedFolder)
            : this(wrappedFolder, true)
        {
        }

		public string Name
		{
			get { return _wrappedFolder.Name; }
		}

		public string Path
		{
			get { return _wrappedFolder.Path; }
		}

		public async Task<IFile> CreateFileAsync(string desiredName, CreationCollisionOption option)
		{
            await EnsureExistsAsync();
            StorageFile wrtFile;
            try
            {
                wrtFile = await _wrappedFolder.CreateFileAsync(desiredName, GetWinRTCreationCollisionOption(option));
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147024713) // 0x800700B7
                {
                    //  File already exists (and potentially other failures, not sure what the HResult represents)
                    throw new IOException(ex.Message, ex);
                }
                throw;
            }
			return new WinRTFile(wrtFile);
		}

		public async Task<IFile> GetFileAsync(string name)
		{
            await EnsureExistsAsync();
			var wrtFile = await _wrappedFolder.GetFileAsync(name);
			return new WinRTFile(wrtFile);
		}

		public async Task<IList<IFile>> GetFilesAsync()
		{
            await EnsureExistsAsync();
			var wrtFiles = await _wrappedFolder.GetFilesAsync();
			var files = wrtFiles.Select(f => new WinRTFile(f)).ToList<IFile>();
			return new ReadOnlyCollection<IFile>(files);
		}

		public async Task<IFolder> CreateFolderAsync(string desiredName, CreationCollisionOption option)
		{
            await EnsureExistsAsync();
			StorageFolder wrtFolder;
            try
            {
                wrtFolder = await _wrappedFolder.CreateFolderAsync(desiredName, GetWinRTCreationCollisionOption(option));
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147024713) // 0x800700B7
                {
                    //  Folder already exists (and potentially other failures, not sure what the HResult represents)
                    throw new IOException(ex.Message, ex);
                }
                throw;
            }
			return new WinRTFolder(wrtFolder, false);
		}

		public async Task<IFolder> GetFolderAsync(string name)
		{
            await EnsureExistsAsync();
			StorageFolder wrtFolder;
            try
            {
                wrtFolder = await _wrappedFolder.GetFolderAsync(name);
            }
            catch (FileNotFoundException ex)
            {
                //  Folder does not exist
                throw new Exceptions.DirectoryNotFoundException(ex.Message, ex);
            }
			return new WinRTFolder(wrtFolder, false);
		}

		public async Task<IList<IFolder>> GetFoldersAsync()
		{
            await EnsureExistsAsync();
			var wrtFolders = await _wrappedFolder.GetFoldersAsync();
			var folders = wrtFolders.Select(f => new WinRTFolder(f, false)).ToList<IFolder>();
			return new ReadOnlyCollection<IFolder>(folders);
		}

		public async Task DeleteAsync()
		{
            await EnsureExistsAsync();

            if (_isRootFolder)
            {
                throw new IOException("Cannot delete root storage folder.");
            }

            await _wrappedFolder.DeleteAsync();
		}

		Windows.Storage.CreationCollisionOption GetWinRTCreationCollisionOption(CreationCollisionOption option)
		{
			if (option == CreationCollisionOption.GenerateUniqueName)
			{
				return Windows.Storage.CreationCollisionOption.GenerateUniqueName;
			}
			else if (option == CreationCollisionOption.ReplaceExisting)
			{
				return Windows.Storage.CreationCollisionOption.ReplaceExisting;
			}
			else if (option == CreationCollisionOption.FailIfExists)
			{
				return Windows.Storage.CreationCollisionOption.FailIfExists;
			}
			else if (option == CreationCollisionOption.OpenIfExists)
			{
				return Windows.Storage.CreationCollisionOption.OpenIfExists;
			}
			else
			{
				throw new ArgumentException("Unrecognized CreationCollisionOption value: " + option);
			}
		}

        async Task EnsureExistsAsync()
        {
            try
            {
                await StorageFolder.GetFolderFromPathAsync(Path);
            }
            catch (FileNotFoundException ex)
            {
                //  Folder does not exist
                throw new Exceptions.DirectoryNotFoundException(ex.Message, ex);
            }
        }
	}
}
