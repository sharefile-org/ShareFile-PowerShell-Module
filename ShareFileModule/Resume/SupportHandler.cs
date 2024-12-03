using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ShareFile.Api.Powershell.Resume
{
    /// <summary>
    /// Object Serializer/De-Serializer class to save and load file
    /// </summary>
    /// <typeparam name="T">Generic type</typeparam>
    class SupportHandler<T> where T : class
    {
        private static readonly object syncFileLock = new object();

        /// <summary>
        /// Load/de-serialize file to object
        /// </summary>
        /// <param name="path">Path + File Name</param>
        /// <returns>File Object</returns>
        public static T Load(string path)
        {
            lock (syncFileLock)
            {
                T serializableObject = null;

                using (Stream textReader = CreateTextReader(path))
                {
                    XmlSerializer xmlSerializer = CreateXmlSerializer();
                    serializableObject = xmlSerializer.Deserialize(textReader) as T;

                    textReader.Close();
                }

                return serializableObject;
            }
        }

        /// <summary>
        /// Save/serialize object to file
        /// </summary>
        /// <param name="serializableObject">Serializable class object</param>
        /// <param name="path">Path + File Name</param>
        public static void Save(T serializableObject, string path)
        {
            lock (syncFileLock)
            {
                using (Stream textWriter = CreateTextWriter(path))
                {
                    XmlSerializer xmlSerializer = CreateXmlSerializer();
                    xmlSerializer.Serialize(textWriter, serializableObject);

                    textWriter.Close();
                }
            }
        }

        #region Private

        private static Stream CreateTextReader(string path)
        {
            Stream textReader = new FileStream(path, FileMode.Open);

            return textReader;
        }

        private static Stream CreateTextWriter(string path)
        {
            Stream textWriter = new FileStream(path, FileMode.Create);
            
            return textWriter;
        }

        private static XmlSerializer CreateXmlSerializer()
        {
            Type ObjectType = typeof(T);

            XmlSerializer xmlSerializer = new XmlSerializer(ObjectType);

            return xmlSerializer;
        }

        #endregion
    }
}
