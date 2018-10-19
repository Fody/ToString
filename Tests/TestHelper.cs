using System.Text;
using System.Xml.Linq;

static class TestHelper
{
    public static XElement PrepareConfig(AttributesConfiguration configuration)
    {
        var configXml = new StringBuilder();
        configXml.Append("<ToString ");
        if (!string.IsNullOrEmpty(configuration.PropertyNameToValueSeparator))
        {
            configXml.AppendFormat("PropertyNameToValueSeparator=\"{0}\" ", configuration.PropertyNameToValueSeparator);
        }

        if (!string.IsNullOrEmpty(configuration.PropertiesSeparator))
        {
            configXml.AppendFormat("PropertiesSeparator=\"{0}\" ", configuration.PropertiesSeparator);
        }

        if (configuration.WrapWithBrackets.HasValue)
        {
            configXml.AppendFormat("WrapWithBrackets=\"{0}\" ", configuration.WrapWithBrackets);
        }

        if (configuration.WriteTypeName.HasValue)
        {
            configXml.AppendFormat("WriteTypeName=\"{0}\" ", configuration.WriteTypeName);
        }

        if (!string.IsNullOrEmpty(configuration.ListStart))
        {
            configXml.AppendFormat("ListStart=\"{0}\" ", configuration.ListStart);
        }

        if (!string.IsNullOrEmpty(configuration.ListEnd))
        {
            configXml.AppendFormat("ListEnd=\"{0}\" ", configuration.ListEnd);
        }

        configXml.Append("/>");

        return XElement.Parse(configXml.ToString());
    }
}