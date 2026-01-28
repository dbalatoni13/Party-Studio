using MPLibrary.GCN;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Xml.Linq;
using Toolbox.Core;

namespace PartyStudioCLI
{
    internal class Program
    {
        static string normal_input_dir = @"C:\Users\kabiskac\Documents\git_contributions\marioparty4\orig\GMPE01_00\files\data";
        static string dol_extracted_dir = @"C:\Users\kabiskac\Documents\git_contributions\marioparty4\build\GMPE01_00\bin";
        static string output_dir = @"C:\Users\kabiskac\Documents\git_contributions\marioparty4\build\releasex86\GMPE01_00\files\data";

        static void Main(string[] args)
        {
            foreach (var file in Directory.GetFiles(normal_input_dir))
            {
                if (file.EndsWith(".bin"))
                {
                    Console.WriteLine("Working on " + file);
                    BatchByteswapBin(file);
                }
            }
            foreach (var file in Directory.GetFiles(dol_extracted_dir))
            {
                if (file.EndsWith(".anm"))
                {
                    Console.WriteLine("Working on " + file);
                    BatchByteswapAnm(file);
                }
            }
            Console.WriteLine("Done");
        }

        static void BatchByteswapBin(string filepath)
        {
            var bin = new MPBIN(filepath);
            for (int i = 0; i < bin.files.Count; i++)
            {
                var file = bin.files[i];
                // Open them and write them out because otherwise the Save function doesn't get called so no byteswapping happens
                if (file.FileName.EndsWith(".atb"))
                {
                    AtbFile atb = new AtbFile(file.FileData);
                    SaveAtbToArchive(file, atb);
                }
                else if (file.FileName.EndsWith(".hsf"))
                {
                    HsfFile hsf = new HsfFile(file.FileData);
                    SaveHsfToArchive(file, hsf);

                }
            }
            string out_file_path = Path.Combine(output_dir, Path.GetFileName(filepath));
            using (var fileStream = new FileStream(out_file_path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                bin.Save(fileStream);
            }
        }

        static void BatchByteswapAnm(string filepath)
        {
            var atb = new AtbFile(filepath);
            string out_file_path = Path.Combine(output_dir, Path.GetFileName(filepath));
            using (var fileStream = new FileStream(out_file_path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                atb.Save(fileStream);
            }
        }

        static void SaveAtbToArchive(MPBIN.FileEntry file, AtbFile atb) {
            var mem = new MemoryStream();
            atb.Save(mem);
            file.FileData = new MemoryStream(mem.ToArray());
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
