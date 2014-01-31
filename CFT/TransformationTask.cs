using System;
using System.IO;
using System.Xml;
using Microsoft.Web.Publishing.Tasks;

namespace BlogTalkRadio.Tools.CFT
{
    public class TransformationTask
    {
        private IXmlTransformationLogger _transformationLogger;


        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationTask"/> class.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="TraceTransformationLogger"/> as the default logger.</remarks>
        public TransformationTask()
            : this(new TraceTransformationLogger())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationTask"/> class.
        /// </summary>
        /// <param name="transformationLogger">The transformation logger.</param>
        public TransformationTask(IXmlTransformationLogger transformationLogger)
        {
            _transformationLogger = transformationLogger;
        }

        /// <summary>
        /// Create new TransformationTask object and set values for <see cref="SourceFilePath"/> and <see cref="TransformFile"/>
        /// </summary>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="transformFilePath">Transformation file path</param>
        public TransformationTask(string sourceFilePath, string transformFilePath)
        {
            SourceFilePath = sourceFilePath;
            TransformFile = transformFilePath;
        }

        /// <summary>
        /// Source file
        /// </summary>
        public string SourceFilePath { get; set; }

        /// <summary>
        /// Transformation file
        /// </summary>
        /// <remarks>
        /// See http://msdn.microsoft.com/en-us/library/dd465326.aspx for syntax of transformation file
        /// </remarks>
        public string TransformFile { get; set; }

        /// <summary>
        /// Make transformation of file <see cref="SourceFilePath"/> with transform file <see cref="TransformFile"/> to <paramref name="destinationFilePath"/>.
        /// </summary>
        /// <param name="destinationFilePath">File path of destination transformation.</param>
        /// <returns>Return true if transformation finish successfully, otherwise false.</returns>
        public void Execute(string destinationFilePath)
        {
            if (string.IsNullOrWhiteSpace(destinationFilePath))
            {
                throw new ArgumentException("Destination file can't be empty.", "destinationFilePath");
            }

            if (string.IsNullOrWhiteSpace(SourceFilePath) || !File.Exists(SourceFilePath))
            {
                throw new FileNotFoundException("Can't find source file.", SourceFilePath);
            }

            if (string.IsNullOrWhiteSpace(TransformFile) || !File.Exists(TransformFile))
            {
                throw new FileNotFoundException("Can't find transform  file.", TransformFile);
            }

            string transformFileContents = File.ReadAllText(TransformFile);

            var document = new XmlDocument();

            try
            {
                document.Load(SourceFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error loading source '{0}': {1}", SourceFilePath, ex.Message), ex);
            }

            var transformation = new XmlTransformation(transformFileContents, false, _transformationLogger);

            try
            {
                bool result = transformation.Apply(document);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error generating '{0}': {1}", destinationFilePath, ex.Message), ex);
            }

            document.Save(destinationFilePath);

        }
    }
}
