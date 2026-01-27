using MPLibrary.GCN;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Xml.Linq;
using Toolbox.Core;

namespace PartyStudioCLI
{
    internal class Program
    {
        static string input_dir = @"C:\Users\kabiskac\Documents\git_contributions\marioparty4\orig\GMPE01_00\files\data";
        static string output_dir = @"C:\Users\kabiskac\Documents\git_contributions\marioparty4\build\releasex86\GMPE01_00\files\data";

        static void Main(string[] args)
        {
            //output_dir = @"C:\Users\Nathan\Desktop\Games\MP4\data";

            foreach (var file in Directory.GetFiles(input_dir))
            {
                if (file.EndsWith(".bin"))
                {
                    Console.WriteLine("Working on " + file);
                    BatchByteswap(file);
                }
            }
            Console.WriteLine("Done");
        }

        static void BatchByteswap(string filepath)
        {
            var bin = new MPBIN(filepath);
            for (int i = 0; i < bin.files.Count; i++)
            {
                var file = bin.files[i];
                // Open them and write them out because otherwise the Save function doesn't get called so no byteswapping happens
                if (file.FileName.EndsWith(".atb"))
                {
                    AtbFile atb = new AtbFile(file.FileData);
                    var mem = new MemoryStream();
                    atb.Save(mem);
                    file.FileData = new MemoryStream(mem.ToArray());
                }
                else if (file.FileName.EndsWith(".hsf"))
                {
                    HsfFile hsf = new HsfFile(file.FileData);
                    var mem = new MemoryStream();
                    hsf.Save(mem);
                    file.FileData = new MemoryStream(mem.ToArray());

                }
            }
            string out_file_path = Path.Combine(output_dir, Path.GetFileName(filepath));
            using (var fileStream = new FileStream(out_file_path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                bin.Save(fileStream);
            }
        }

        static void ProcessExportAny(string filePath)
        {
            MPBIN bin = new MPBIN(filePath);

            string folder = Path.Combine("ModelDump", Path.GetFileNameWithoutExtension(filePath));

            for (int i = 0; i < bin.files.Count; i++)
            {
                if (bin.files[i].FileName.Contains("MESH"))
                {
                    try
                    {
                        ExportModelHSF(bin.files[i], Path.Combine(folder, $"File{i}"));
                    }
                    catch
                    {

                    }
                }
            }
        }

        static void ProcessExport(string filePath, int num, int start_idx = 0)
        {
            MPBIN bin = new MPBIN(filePath);

            string folder = Path.Combine("ModelDump", Path.GetFileNameWithoutExtension(filePath));

            for (int i = 0; i < num; i++)
                ExportModelHSF(bin.files[start_idx + i], Path.Combine(folder, $"LOD{i}"));
        }

        static void ExportModelHSF(MPBIN.FileEntry file, string folder)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            HsfFile hsf = new HsfFile(file.FileData);

            HSFModelImporter.Export(hsf, Path.Combine(folder, "model.dae"));
        }

        static void ProcessModelImport(string filePath, string import_file)
        {
            string name = Path.GetFileName(filePath);

            MPBIN bin = new MPBIN(filePath);

            //LOD 0 (BOARD & MINI GAME SELECTION SCREEN)
            ImportModelHSF(bin.files[0], import_file, 0);
            //LOD 1 (MINI GAMES)
            ImportModelHSF(bin.files[1], import_file, 1);
            //LOD 2 (MINI GAMES LOW POLY)
            ImportModelHSF(bin.files[2], import_file, 2);

            Console.WriteLine($"Compressing BIN");

            bin.Save(Path.Combine(output_dir, name));

            Console.WriteLine($"BIN Saved!");
        }

        static void ImportModelHSF(MPBIN.FileEntry file, string import_file, int file_idx)
        {
            HsfFile hsf = new HsfFile(file.FileData);

            var imported = HSFModelImporter.Import(import_file, hsf, new HSFModelImporter.ImportSettings()
            {

            });

            hsf.ObjectNodes.Clear();
            hsf.ObjectNodes.AddRange(imported.ObjectNodes);

            hsf.Meshes.Clear();
            hsf.Meshes.AddRange(imported.Meshes);

            hsf.Materials.Clear();
            hsf.Materials.AddRange(imported.Materials);

            hsf.Textures.Clear();
            hsf.Textures.AddRange(imported.Textures);

            hsf.SkeletonData = imported.SkeletonData;
            hsf.MatrixData = imported.MatrixData;

            SaveHsfToArchive(file, hsf);
        }

        static void BatchExportATB(string filePath)
        {
            List<AtbFile> files = new List<AtbFile>();

            MPBIN bin = new MPBIN(filePath);

            int atb_file = 0;
            foreach (var file in bin.Files)
            {
                if (file.FileName.EndsWith("atb"))
                    files.Add(new AtbFile(file.FileData) { FileIndex = atb_file });

                atb_file++;
            }

            if (files.Count > 0)
            {
                string folder = Path.Combine("Dump");
                string name = Path.GetFileNameWithoutExtension(filePath);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                foreach (var atb in files)
                {
                    int sprite_idx = 0;

                    foreach (var tex in atb.Textures)
                    {
                        string path = Path.Combine(folder, $"{name}_file{atb.FileIndex}_sprite{sprite_idx++}.png");
                        DumpATB(tex, path);

                        try
                        {
                        }
                        catch
                        {

                        }
                    }
                    atb_file++;
                }
            }
        }

        static void DumpATB(AtbTextureInfo tex, string filePath)
        {
            var gcFormat = FormatList[tex.Format & 0xF];
            var gcnPalette = Decode_Gamecube.PaletteFormats.RGB5A3;
            var rgba = gctex.Decode(tex.ImageData, tex.Width, tex.Height, (uint)gcFormat,
                tex.PaletteData, (uint)gcnPalette);

            var image = Image.LoadPixelData<Rgba32>(rgba, tex.Width, tex.Height);
            image.SaveAsPng(filePath);
        }

        public static Dictionary<int, Decode_Gamecube.TextureFormats> FormatList = new Dictionary<int, Decode_Gamecube.TextureFormats>()
            {
            { 0x00, Decode_Gamecube.TextureFormats.RGBA32 },
            { 0x01, Decode_Gamecube.TextureFormats.RGB5A3 },
            { 0x02, Decode_Gamecube.TextureFormats.RGB5A3 },
            { 0x03, Decode_Gamecube.TextureFormats.C8 },
            { 0x04, Decode_Gamecube.TextureFormats.C4 },
            { 0x05, Decode_Gamecube.TextureFormats.IA8 },
            { 0x06, Decode_Gamecube.TextureFormats.IA4 },
            { 0x07, Decode_Gamecube.TextureFormats.I8 },
            { 0x08, Decode_Gamecube.TextureFormats.I4 },
            { 0x09, Decode_Gamecube.TextureFormats.IA8 },
            { 0x0A, Decode_Gamecube.TextureFormats.CMPR },
           };

        static void ProcessMotionBatch(string filePath)
        {
            string name = Path.GetFileName(filePath);

            MPBIN bin = new MPBIN(filePath);
            ProcessMotionBatch(bin);

            Console.WriteLine($"Compressing BIN");

            bin.Save(Path.Combine(output_dir, name));

            Console.WriteLine($"BIN Saved!");
        }

        static void ProcessMotionBatch(MPBIN bin)
        {
            int id = 0;
            foreach (MPBIN.FileEntry file in bin.Files)
            {
                Console.WriteLine($"Processing HSF {id++}");
                HsfFile hsf = new HsfFile(file.FileData);
                ProcessMotion(hsf);
                SaveHsfToArchive(file, hsf);
            }
        }

        static void ProcessMotion(HsfFile hsf)
        {
            foreach (var motion in hsf.MotionData.Animations)
            {
                foreach (var track in motion.GetAllTracks())
                {
                    //reset index based motion tracks
                    //These generally will break custom models, so just blank them out
                    if (track.TrackMode == TrackMode.Attriubute)
                        track.ValueIdx = 0;
                    if (track.TrackMode == TrackMode.Material)
                        track.ValueIdx = 0;

                    if (track.ValueIdx != 0)
                        Console.WriteLine($"{track.TrackMode} {(AttributeTrackEffect)track.TrackEffect} Idx {track.Constant}");
                }
            }
        }

        static void SaveHsfToArchive(MPBIN.FileEntry file, HsfFile hsf)
        {
            //Save to archive
            var mem = new MemoryStream();
            hsf.Save(mem);
            file.FileData = new MemoryStream(mem.ToArray());
        }
    }
}
