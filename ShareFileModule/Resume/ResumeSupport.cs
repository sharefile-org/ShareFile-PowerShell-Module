using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ShareFile.Api.Powershell.Properties;

namespace ShareFile.Api.Powershell.Resume
{
    /// <summary>
    /// ResumeSupport class to facilitate the resume last Copy-SfItem command if failed somehow
    /// It will use ".progressfile" file to keep track of current command and successfully copied files
    /// If command breaks and failed to complete due to disconnection then on next copy operation it will check progress file and prompt user
    /// </summary>
    class ResumeSupport
    {
        private String XML_FILE_NAME = @".progressfile";
        private ProgressFile progressObject;
        private bool flagResume;

        /// <summary>
        /// Initialize/Load the progressfile and de-serialize the object
        /// </summary>
        public ResumeSupport()
        {

            string directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            XML_FILE_NAME = Path.Combine(directory, Resources.AppName, XML_FILE_NAME);
            flagResume = true;
            progressObject = LoadFile();
        }

        /// <summary>
        /// Starting a new Copy command
        /// </summary>
        /// <param name="source">Source Paths</param>
        /// <param name="target">Destination Path</param>
        /// <param name="force">Force Replace</param>
        /// <param name="details">Upload Specification Request Details</param>
        public void Start(String[] source, String target, bool force, String details)
        {
            progressObject = new ProgressFile();

            progressObject.ArgumentSource = source;
            progressObject.ArgumentTarget = target;
            progressObject.ArgumentForce = force;
            progressObject.ArgumentDetails = details;

            SaveFile(progressObject);
        }

        /// <summary>
        /// Mark file as complete (copy done)
        /// </summary>
        /// <param name="fileName">File Name</param>
        public void MarkFileStatus(String fileName)
        {
            progressObject.CompletedFiles.Add(fileName);
            progressObject.IsPending = true;

            SaveFile(progressObject);
        }

        /// <summary>
        /// Check the file status if copying complete or not
        /// </summary>
        /// <param name="fileName">File Name</param>
        /// <returns>True if file is succssfully copied</returns>
        public bool CheckFileStatus(String fileName)
        {
            return progressObject.CompletedFiles.Contains(fileName);
        }

        /// <summary>
        /// Command is completed
        /// Clean up the .progressfile and mark it completed
        /// </summary>
        public void End()
        {
            progressObject.CompletedFiles = new System.Collections.ArrayList();
            progressObject.IsPending = false;

            SaveFile(progressObject);
        }

        /// <summary>
        /// Check if last execution is completed or disconnected
        /// </summary>
        public bool IsPending
        {
            get
            {
                return flagResume && progressObject.IsExist && progressObject.IsPending;
            }
        }

        /// <summary>
        /// Get source path
        /// </summary>
        public String[] GetPath
        {
            get
            {
                return progressObject.ArgumentSource;
            }
        }

        /// <summary>
        /// Get destination path
        /// </summary>
        public String GetDestination
        {
            get
            {
                return progressObject.ArgumentTarget;
            }
        }

        /// <summary>
        /// Get fource argument
        /// </summary>
        public bool GetForce
        {
            get
            {
                return progressObject.ArgumentForce;
            }
        }

        /// <summary>
        /// Get details argument
        /// </summary>
        public String GetDetails
        {
            get
            {
                return progressObject.ArgumentDetails;
            }
        }

        public void UnmarkResumeFlag()
        {
            this.flagResume = false;
        }

        /// <summary>
        /// Save/serialize progress to ".progressfile" file
        /// It will be called after each operation i.e. file completion, command completion & new command
        /// </summary>
        /// <param name="progressObject">Process file object</param>
        private void SaveFile(ProgressFile progressObject)
        {
            SupportHandler<ProgressFile>.Save(progressObject, XML_FILE_NAME);
        }

        /// <summary>
        /// Load/de-serialize progress of command from ".progressfile" to and object
        /// </summary>
        /// <returns>ProgressFile object</returns>
        private ProgressFile LoadFile()
        {
            ProgressFile progressObject = new ProgressFile();

            if (File.Exists(XML_FILE_NAME) == true)
            {
                progressObject = SupportHandler<ProgressFile>.Load(XML_FILE_NAME);
                progressObject.IsExist = true;
            }

            return progressObject;
        }
    }
}
