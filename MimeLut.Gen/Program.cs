﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace MimeLut.Gen
{
   class Program
   {
      private static readonly char[] SymbolsToReplacePropertyName = { '-', '.', '/' };
      class Gen
      {
         private readonly StringBuilder p_data;
         private readonly string p_intend;
         public Gen() => p_data = new StringBuilder();

         public Gen(StringBuilder _builder, string _intend)
         {
            p_data = _builder;
            p_intend = _intend;
         }

         public Gen this[Func<Gen, Gen> _do] => _do(this);
         public Gen this[string _line]
         {
            get
            {
               switch (_line)
               {
                  case "{":
                     p_data.AppendLine(p_intend == null ? "{" : p_intend + "{");
                     return new Gen(p_data, p_intend == null ? "\t" : p_intend + "\t");
                  case "}":
                     p_data.AppendLine(p_intend == null ? "}" : p_intend.Substring(0, p_intend.Length - 1) + "}");
                     return new Gen(p_data, p_intend?.Substring(0, p_intend.Length - 1));
               }                  
               foreach (var line in _line.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                  p_data.AppendLine(p_intend == null ? line : p_intend + line);
               return this;
            }
         }
         public override string ToString() => p_data.ToString();
         public static implicit operator string(Gen _g) => _g.ToString();
      }

      static int Main(string[] _args)
      {
         var assembly = typeof(Program).Assembly;
         var fore = Console.ForegroundColor;
         void restore() => Console.ForegroundColor = fore;
         void error() => Console.ForegroundColor = ConsoleColor.Red;
         void info() => Console.ForegroundColor = ConsoleColor.White;
         void skipping() => Console.ForegroundColor = ConsoleColor.DarkGray;
         void found() => Console.ForegroundColor = ConsoleColor.Green;
         void printUsing() => Console.WriteLine($"{assembly.GetName().Name} <output_path> <class_name>");

         if (_args.Length < 2)
         {
            printUsing();
            return -1;
         }

         var outputFile = _args[0];
         if (!ParseClassName(_args[1], out var nameSpace, out var className))
         {
            error();
            Console.WriteLine("Wrong arguments. class_name is incorrect");
            restore();
            printUsing();
            return -1;
         }

         Console.WriteLine($"Output: {outputFile}");
         Console.WriteLine($"Classname: {className}");
         Console.WriteLine($"Namespace: {nameSpace}");

         var home = Path.GetDirectoryName(assembly.Location);
         Console.WriteLine($"Home: {home}");


         var lut = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);
         var extLut = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

         var deserializer = new DeserializerBuilder()
            .WithTagMapping("!ruby/object:MIME::Type", typeof(MimeType))
            .Build();
         foreach (var file in Directory.EnumerateFiles(Path.Combine(home, "types"), "*.yaml"))
         {
            info();
            Console.WriteLine($"Reading: {file}");
            restore();

            var types = deserializer.Deserialize<MimeType[]>(File.ReadAllText(file));
            foreach (var type in types)
            {
               if(type.Obsolete)
                  continue;
               if (!(type.Extensions?.Length > 0))
               {
                  lut.Add(type.ContentType, type.Extensions);
                  continue;
               }

               lut.Add(type.ContentType, type.Extensions);
               foreach (var e in type.Extensions)
                  extLut[e] = type.ContentType;
               found();
               Console.WriteLine($"Found '{type.ContentType}' -> '{string.Join(",", type.Extensions)}");
               restore();
            }
            info();
            Console.WriteLine($"Parsed: {file}");
            restore();
         }



         info();
         Console.WriteLine($"Generating C#...");
         restore();

         var code = new Gen()
            [$"namespace {nameSpace}"]
            ["{"]
               [$"public static class {className}"]
               ["{"]
                  [_ => lut.Keys.Aggregate(_, (_gen, _mime) => _gen[$"public static string {AppropriatePropertyName(_mime)} {{get;}} = \"{_mime}\";"])] // Properties
                  
                  ["public static bool TryFindMimeTypeByExtension(string _extensionWithoutDot, out string _mime)"] // TryFind Mime Type By Extension
                  ["{"]
                     ["switch(_extensionWithoutDot.ToLower())"]
                     ["{"]
                        [_ => extLut.Aggregate(_, (_gen, _pair) =>_gen
                           [$"case \"{_pair.Key.ToLower()}\":"]
                           ["{"]
                              [$"_mime = {AppropriatePropertyName(_pair.Value)};"]
                              ["return true;"]
                           ["}"]
                        )]
                     ["}"]
                     ["_mime = null;"]
                     ["return false;"]
                  ["}"]
                  ["public static string FindMimeTypeByExtension(string _extensionWithoutDot)"] // Find Mime Type By Extension
                  ["{"]
                     ["if(TryFindMimeTypeByExtension(_extensionWithoutDot, out var _mime)) return _mime;"]
                     ["throw new System.NotSupportedException();"]
                  ["}"]
               ["}"]
            ["}"]
            .ToString();

         Console.WriteLine(code);

         info();
         Console.WriteLine($"Generated C#");
         Console.WriteLine($"Writting to {outputFile}");
         restore();

         File.WriteAllText(outputFile, code);
         info();
         Console.WriteLine($"done");
         restore();
         return 0;
      }

      private static string AppropriatePropertyName(string _mime)
      {
         var res = new char[_mime.Length];
         var j = 0;

         var nextIsUpper = true;
         for (var i = 0; i < _mime.Length; i++)
         {
            var ch = _mime[i];
            if (SymbolsToReplacePropertyName.Contains(ch) )
            {
               nextIsUpper = true;
               continue;
            }
            res[j] = nextIsUpper ? char.ToUpper(ch) : ch;
            nextIsUpper = false;

            if (ch == '+')
               nextIsUpper = true;
            j++;
         }

         return new string(res, 0, j).Replace("+","Plus");
      }

      private static bool ParseClassName(string _fullClassname, out string _namespace, out string _classname)
      {
         var ind = _fullClassname.LastIndexOf('.');
         if (ind <= 0)
         {
            _namespace = null;
            _classname = null;
            return false;
         }

         _namespace = _fullClassname.Substring(0, ind);
         _classname = _fullClassname.Substring(ind + 1);
         return true;
      }
   }
}