using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ShareFile.Api.Powershell.Resume
{
    /// <summary>
    /// ProgressFile Serializable class
    /// Its image will be saved in file
    /// </summary>
    [XmlRootAttribute("ProgressFile", Namespace = "", IsNullable = false)]
    public class ProgressFile
    {
        public ProgressFile()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        [XmlArray("ArgumentSource"), XmlArrayItem("Path", typeof(string))]
        public string[] ArgumentSource = new String[]{};
        
        /// <summary>
        /// 
        /// </summary>
        public string ArgumentTarget;
        
        /// <summary>
        /// 
        /// </summary>
        public bool ArgumentForce;
        
        /// <summary>
        /// 
        /// </summary>
        public string ArgumentDetails;
        
        /// <summary>
        /// 
        /// </summary>
         [XmlIgnore]
        public bool IsExist;
        
        /// <summary>
        /// 
        /// </summary>
        public bool IsPending;

        /// <summary>
        /// 
        /// </summary>
        [XmlArray("CompletedFiles"), XmlArrayItem("File", typeof(string))]
        public System.Collections.ArrayList CompletedFiles = new System.Collections.ArrayList();

        
    }
}
