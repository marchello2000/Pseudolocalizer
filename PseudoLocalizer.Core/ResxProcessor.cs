namespace PseudoLocalizer.Core
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using YamlDotNet.RepresentationModel;

    /// <summary>
    /// Applies transforms to string values in Resx resource files.
    /// </summary>
    public class ResxProcessor
    {
        /// <summary>
        /// Event raised when a string value to be transformed is found.
        /// </summary>
        public event EventHandler<TransformStringEventArgs> TransformString;

        /// <summary>
        /// Transform: read from an input stream and write to an output stream.
        /// </summary>
        public virtual void Transform(Stream inputStream, Stream outputStream)
        {
            var document = new XmlDocument();
            document.PreserveWhitespace = true;
            document.Load(inputStream);

            foreach (XmlNode node in document.SelectNodes("/root/data/value"))
            {
                var child = node.FirstChild;
                if (child != null && child.NodeType == XmlNodeType.Text)
                {
                    var original = child.Value;
                    var args = new TransformStringEventArgs { Value = original };
                    OnTransformString(args);

                    if (args.Value != original)
                    {
                        child.Value = args.Value;
                    }
                }
            }

            using (var xmlWriter = XmlWriter.Create(outputStream))
            {
                document.WriteTo(xmlWriter);
            }
        }

        protected void OnTransformString(TransformStringEventArgs args)
        {
            var handler = TransformString;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }


    public class YmlProcessor : ResxProcessor
    {
        private void ProcessNodes(YamlMappingNode mapping)
        {
            foreach (var entry in mapping.Children)
            {
                if (entry.Value.NodeType == YamlNodeType.Scalar)
                {
                    YamlScalarNode node = (YamlScalarNode)entry.Value;

                    var args = new TransformStringEventArgs { Value = node.Value };
                    OnTransformString(args);

                    node.Value = args.Value;
                }
                else if (entry.Value.NodeType == YamlNodeType.Mapping)
                {
                    ProcessNodes((YamlMappingNode)entry.Value);
                }
            }
        }

        public override void Transform(Stream inputStream, Stream outputStream)
        {
            using (TextReader reader = new StreamReader(inputStream))
            using (TextWriter writer = new StreamWriter(outputStream))
            {
                var yaml = new YamlStream();
                yaml.Load(reader);

                foreach (YamlDocument doc in yaml.Documents)
                {
                    var mapping = (YamlMappingNode)doc.RootNode;
                    ProcessNodes(mapping);
                }

                yaml.Save(writer, false);
            }
        }
    }
}
