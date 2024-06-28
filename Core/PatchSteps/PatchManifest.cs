﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;
using QuestPatcher.Axml;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    // based a lot on
    // https://github.com/Lauriethefish/QuestPatcher/blob/main/QuestPatcher.Core/Patching/PatchingManager.cs
    internal class PatchManifest : IPatchStep
    {
        private static readonly Uri AndroidNamespaceUri = new Uri("http://schemas.android.com/apk/res/android");

        private const int NameAttributeResourceId = 16842755;
        private const int RequiredAttributeResourceId = 16843406;
        private const int DebuggableAttributeResourceId = 16842767;
        private const int LegacyStorageAttributeResourceId = 16844291;
        private const int ExtractNativeLibsAttributeResourceId = 16844010;

        private static readonly string[] StandardPermissions = new string[]
        {
            "android.permission.READ_EXTERNAL_STORAGE",
            "android.permission.WRITE_EXTERNAL_STORAGE",
            "android.permission.MANAGE_EXTERNAL_STORAGE",
            "android.permission.RECORD_AUDIO",
            "android.permission.INTERNET",
            "com.oculus.permission.HAND_TRACKING",
            // TODO: Is this one actually required? (QuestPatcher adds it)
            "oculus.permission.handtracking",
        };

        private Patcher _patcher;
        private IPatchLogger _logger;

        public bool Run(Patcher patcher)
        {
            _patcher = patcher;
            _logger = patcher._logger;

            PatchAPK(patcher._info.OutputBaseApkPath);
            if (patcher._info.OutputLibApkPath != null)
                PatchAPK(patcher._info.OutputLibApkPath);

            return true;
        }

        private void PatchAPK(string apkPath)
        {
            using ZipFile archive = new ZipFile(apkPath);

            ZipEntry manifestEntry = archive.Entries.First(a => a.FileName == "AndroidManifest.xml");
            using Stream manifestStream = manifestEntry.OpenReader();
            using MemoryStream memoryStream = new MemoryStream();

            manifestStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            AxmlElement manifest = AxmlLoader.LoadDocument(memoryStream);
            AddStandardPermissions(manifest);
            AddHandTrackingFeature(manifest);
            AddApplicationFlags(manifest);

            using MemoryStream saveStream = new MemoryStream();
            AxmlSaver.SaveDocument(saveStream, manifest);
            saveStream.Position = 0;

            archive.UpdateEntry(manifestEntry.FileName, saveStream);
            archive.Save();
        }

        private void AddStandardPermissions(AxmlElement manifest)
        {
            ISet<string> existingPermissions = GetExistingChildren(manifest, "uses-permission");

            foreach (string permission in StandardPermissions)
            {
                if (existingPermissions.Contains(permission)) { continue; } // Do not add existing permissions

                _logger.Log($"Adding permission {permission}");
                AxmlElement permElement = new AxmlElement("uses-permission");
                AddNameAttribute(permElement, permission);
                manifest.Children.Add(permElement);
            }
        }

        private void AddHandTrackingFeature(AxmlElement manifest)
        {
            AxmlElement element = new AxmlElement("uses-feature");
            AddNameAttribute(element, "oculus.software.handtracking");
            element.Attributes.Add(new AxmlAttribute("required", AndroidNamespaceUri, RequiredAttributeResourceId, "false"));
            manifest.Children.Add(element);
        }

        private void AddApplicationFlags(AxmlElement manifest)
        {
            AxmlElement appElement = manifest.Children.Single(element => element.Name == "application");
            if (!appElement.Attributes.Any(attribute => attribute.Name == "debuggable"))
            {
                _logger.Log("Adding debuggable flag");
                appElement.Attributes.Add(new AxmlAttribute("debuggable", AndroidNamespaceUri, DebuggableAttributeResourceId, true));
            }

            if (!appElement.Attributes.Any(attribute => attribute.Name == "requestLegacyExternalStorage"))
            {
                _logger.Log("Adding legacy external storage flag");
                appElement.Attributes.Add(new AxmlAttribute("requestLegacyExternalStorage", AndroidNamespaceUri, LegacyStorageAttributeResourceId, true));
            }

            // This has only been an issue for split APKs
            if (_patcher._args.IsSplit)
            {
                _logger.Log("Patching extract native libraries flag");
                AxmlAttribute extract = appElement.Attributes.FirstOrDefault(attribute => attribute.Name == "extractNativeLibs");
                if (extract != null)
                    extract.Value = true;
                else
                {
                    appElement.Attributes.Add(new AxmlAttribute("extractNativeLibs", AndroidNamespaceUri, ExtractNativeLibsAttributeResourceId, true));
                }
            }
        }

        private void AddNameAttribute(AxmlElement element, string name)
        {
            element.Attributes.Add(new AxmlAttribute("name", AndroidNamespaceUri, NameAttributeResourceId, name));
        }

        private ISet<string> GetExistingChildren(AxmlElement manifest, string childNames)
        {
            HashSet<string> result = new HashSet<string>();

            foreach (AxmlElement element in manifest.Children)
            {
                if (element.Name != childNames) { continue; }

                List<AxmlAttribute> nameAttributes = element.Attributes.Where(attribute => attribute.Namespace == AndroidNamespaceUri && attribute.Name == "name").ToList();
                // Only add children with the name attribute
                if (nameAttributes.Count > 0) { result.Add((string)nameAttributes[0].Value); }
            }

            return result;
        }
    }
}
