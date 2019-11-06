using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace MimeLut.Gen
{
   class MimeType
   {
      [YamlMember(Alias = "content-type")]
      public string ContentType { get; set; }

      [YamlMember(Alias = "extensions")]
      public string[] Extensions { get; set; }

      [YamlMember(Alias = "encoding")]
      public string Encoding { get; set; }     
      
      [YamlMember(Alias = "docs")]
      public string Docs { get; set; }

      [YamlMember(Alias = "friendly")]
      public Dictionary<string,string> Friendly { get; set; }


      [YamlMember(Alias = "xrefs")]
      public Dictionary<string,string[]> XRefs { get; set; }

      [YamlMember(Alias = "registered")]
      public bool Registered { get; set; }

      [YamlMember(Alias = "obsolete")]
      public bool Obsolete { get; set; }

      [YamlMember(Alias = "signature")]
      public bool Signature { get; set; }

      [YamlMember(Alias = "use-instead")]
      public string UseInstead { get; set; }
   }
}