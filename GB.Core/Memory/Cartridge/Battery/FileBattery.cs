using System.IO;
using System.Text;

namespace GB.Core.Memory.Cartridge.Battery
{
    internal sealed class FileBattery : IBattery
    {
        private readonly string _ramFilePath;
        private readonly FileStream? _file;

        public FileBattery(Cartridge cartridge)
        {
            if (string.IsNullOrEmpty(cartridge.FilePath))
            {
                _ramFilePath = "";
                _file = null;
                return;
            }

            _ramFilePath = Path.Combine(
                Path.GetDirectoryName(cartridge.FilePath)!, 
                Path.GetFileNameWithoutExtension(cartridge.FilePath) + ".sav");
            _file = new FileStream(_ramFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public void LoadRam(int[] ram)
        {
            if (_file is null)
            {
                return;
            }

            _file.Seek(0, SeekOrigin.Begin);
            using var reader = new BinaryReader(_file, Encoding.UTF8, true);
            LoadRam(reader, ram);
        }

        public void LoadRamWithClock(int[] ram, long[] clockData)
        {
            if (_file is null)
            {
                return;
            }

            _file.Seek(0, SeekOrigin.Begin);
            using var reader = new BinaryReader(_file, Encoding.UTF8, true);
            LoadRam(reader, ram);
            LoadClock(reader, clockData);
        }

        private void LoadRam(BinaryReader reader, int[] ram)
        {
            var i = 0;

            try
            {
                while (i < ram.Length)
                {
                    ram[i++] = reader.ReadInt32();
                }
            }
            catch (EndOfStreamException) { }
            catch (IOException) { }
        }

        private void LoadClock(BinaryReader reader, long[] clockData)
        {
            var i = 0;

            try
            {
                while (i < clockData.Length)
                {
                    clockData[i++] = reader.ReadInt64();
                }
            }
            catch (EndOfStreamException) { }
            catch (IOException) { }
        }

        public void SaveRam(int[] ram)
        {
            if (_file is null)
            {
                return;
            }

            _file.Seek(0, SeekOrigin.Begin);
            using var writer = new BinaryWriter(_file, Encoding.UTF8, true);
            SaveRam(writer, ram);
        }

        public void SaveRamWithClock(int[] ram, long[] clockData)
        {
            if (_file is null)
            {
                return;
            }

            _file.Seek(0, SeekOrigin.Begin);
            using var writer = new BinaryWriter(_file, Encoding.UTF8, true);
            SaveRam(writer, ram);
            SaveClock(writer, clockData);
        }

        private void SaveRam(BinaryWriter writer, int[] ram)
        {
            var i = 0;

            try
            {
                while (i < ram.Length)
                {
                    writer.Write(ram[i++]);
                }
            }
            catch (IOException) { }
        }

        private void SaveClock(BinaryWriter writer, long[] clockData)
        {
            var i = 0;

            try
            {
                while (i < clockData.Length)
                {
                    writer.Write(clockData[i++]);
                }
            }
            catch (IOException) { }
        }

        public void Dispose()
        {
            if (_file is null)
            {
                return;
            }

            _file.Close();
            _file.Dispose();
        }
    }
}
