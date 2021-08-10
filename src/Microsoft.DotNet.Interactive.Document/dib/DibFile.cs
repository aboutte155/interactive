﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Dib
{
    public static class Document
    {
        private const string InteractiveNotebookCellSpecifier = "#!";

        public static Encoding Encoding => new UTF8Encoding(false);

        public static InteractiveDocument Parse(string content, string defaultLanguage, IDictionary<string, string> kernelLanguageAliases)
        {
            if (kernelLanguageAliases == null)
            {
                throw new ArgumentNullException(nameof(kernelLanguageAliases));
            }

            var lines = StringExtensions.SplitAsLines(content);

            var cells = new List<InteractiveDocumentElement>();
            var currentLanguage = defaultLanguage;
            var currentCellLines = new List<string>();

            InteractiveDocumentElement CreateCell(string cellLanguage, IEnumerable<string> cellLines)
            {
                return new(cellLanguage, string.Join("\n", cellLines));
            }

            void AddCell()
            {
                // trim leading blank lines
                while (currentCellLines.Count > 0 && string.IsNullOrEmpty(currentCellLines[0]))
                {
                    currentCellLines.RemoveAt(0);
                }

                // trim trailing blank lines
                while (currentCellLines.Count > 0 && string.IsNullOrEmpty(currentCellLines[^1]))
                {
                    currentCellLines.RemoveAt(currentCellLines.Count - 1);
                }

                if (currentCellLines.Count > 0)
                {
                    cells.Add(CreateCell(currentLanguage, currentCellLines));
                }
            }

            foreach (var line in lines)
            {
                if (line.StartsWith(InteractiveNotebookCellSpecifier))
                {
                    var cellLanguage = line.Substring(InteractiveNotebookCellSpecifier.Length);
                    if (kernelLanguageAliases.TryGetValue(cellLanguage, out cellLanguage))
                    {
                        // recognized language, finalize the current cell
                        AddCell();

                        // start a new cell
                        currentLanguage = cellLanguage;
                        currentCellLines.Clear();
                    }
                    else
                    {
                        // unrecognized language, probably a magic command
                        currentCellLines.Add(line);
                    }
                }
                else
                {
                    currentCellLines.Add(line);
                }
            }

            // finalize last cell
            AddCell();

            // ensure there's at least one cell available
            if (cells.Count == 0)
            {
                cells.Add(CreateCell(defaultLanguage, Array.Empty<string>()));
            }

            return new InteractiveDocument(cells.ToArray());
        }

        public static InteractiveDocument Read(Stream stream, string defaultLanguage,
            IDictionary<string, string> kernelLanguageAliases)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = reader.ReadToEnd();
            return Parse(content, defaultLanguage, kernelLanguageAliases);
        }

        public static async Task<InteractiveDocument> ReadAsync(Stream stream, string defaultLanguage,
            IDictionary<string, string> kernelLanguageAliases)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = await reader.ReadToEndAsync();
            return Parse(content, defaultLanguage, kernelLanguageAliases);
        }

        public static string ToDibContent(this InteractiveDocument interactiveDocument, string newline = "\n")
        {
            var lines = new List<string>();

            foreach (var cell in interactiveDocument.Elements)
            {
                var cellLines = StringExtensions.SplitAsLines(cell.Contents).SkipWhile(l => l.Length == 0).ToList();
                while (cellLines.Count > 0 && cellLines[^1].Length == 0)
                {
                    cellLines.RemoveAt(cellLines.Count - 1);
                }

                if (cellLines.Count > 0)
                {
                    lines.Add($"{InteractiveNotebookCellSpecifier}{cell.Language}");
                    lines.Add("");
                    lines.AddRange(cellLines);
                    lines.Add("");
                }
            }

            var content = string.Join(newline, lines);
            return content;
        }

        public static void Write(InteractiveDocument interactiveDocument, string newline, Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding, 1024, true);
            Write(interactiveDocument, newline, writer);
            writer.Flush();
        }

        public static void Write(InteractiveDocument interactiveDocument, string newline, TextWriter writer)
        {
            var content = interactiveDocument.ToDibContent(newline);
            writer.Write(content);
        }
        
    }
}