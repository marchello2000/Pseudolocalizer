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
            TextReader reader = new StreamReader(inputStream);
            TextWriter writer = new StreamWriter(outputStream);

            var yaml = new YamlStream();
            yaml.Load(reader);

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            ProcessNodes(mapping);

            yaml.Save(writer, false);
            /*






            while (!reader.EndOfStream)
            {
                string inLine = reader.ReadLine();
                string outLine;

                if ((inLine == "---") || (inLine.StartsWith("#")))
                {
                    outLine = inLine;
                }
                else
                {
                    int index = inLine.IndexOf(":");
                    string key = inLine.Substring(0, index - 1);

                    string value = null;

                    if (index < inLine.Length)
                    {
                        value = inLine.Substring(index + 1);
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        outLine = $"{key}:";
                    }
                    else
                    {
                        var args = new TransformStringEventArgs { Value = value };
                        OnTransformString(args);
                        outLine = $"{key}: {args.Value}";
                    }
                }

                writer.WriteLine(outLine);
            }
            */
        }

    }
}
