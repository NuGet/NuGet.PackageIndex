using System.Text;

namespace Nuget.PackageIndex.Models
{
    public class PackageInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Name)
                         .Append(Version);

            return stringBuilder.ToString();
        }
    }
}
