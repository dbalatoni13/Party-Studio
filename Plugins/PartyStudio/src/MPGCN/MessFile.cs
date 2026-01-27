using ImGuiNET;
using MapStudio.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;

namespace MPLibrary.GCN
{
    public class MessFile : FileEditor, IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Mario Party GCN Message" };
        public string[] Extension { get; set; } = new string[] { "*.bin", "*.dat" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo,  Stream stream) {
            return GetVersion(fileInfo.FileName) != 0;
        }

        MessFileData messFile;
        private string converted_json = "";

        public void Load(Stream stream) 
        {
            messFile = new MessFileData(stream, Encoding.UTF8, GetVersion(FileInfo.FileName));
            converted_json = MessFileData.Export(messFile);

            Root.ContextMenus.Add(new MenuItemModel("Export", () =>
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.FileName = Path.GetFileNameWithoutExtension(Root.Header) + ".json";
                dlg.AddFilter(".json", "json");
                dlg.SaveDialog = true;
                if (dlg.ShowDialog()) {
                    File.WriteAllText(dlg.FilePath, converted_json);
                }
            }));
            Root.ContextMenus.Add(new MenuItemModel("Replace", () =>
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.AddFilter(".json", "json");
                if (dlg.ShowDialog())
                {
                    messFile = MessFileData.Import(dlg.FilePath);
                    converted_json = MessFileData.Export(messFile);
                }
            }));

            Root.TagUI.UIDrawer += delegate
            {
                ImGui.TextUnformatted(converted_json);
            };
        }



        public void Save(System.IO.Stream stream) {
            messFile.Save(stream, messFile.Version, Encoding.UTF8);
        }

        private uint GetVersion(string fileName)
        {
            switch (fileName)
            {
                case "board.dat":
                case "board_e.dat":
                case "mini.dat":
                case "mini_e.dat":
                    return 4;
                case "messdata_eng.bin":
                case "messdata_fra.bin":
                case "messdata_ger.bin":
                case "messdata_ita.bin":
                case "messdata_spa.bin":
                case "messdata.bin":
                    return 6; //all other game versions. 5 and up are the same structure
            }
            return 0;
        }
    }
}
