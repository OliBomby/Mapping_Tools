namespace Mapping_Tools_Core.ExternalFileUtil.Reaper {
    public class ReaperEditor : Editor
    {
        public ReaperProject project => (ReaperProject)TextFile;

        public ReaperEditor(List<string> lines) 
            => TextFile = new ReaperProject(lines);
        
        public ReaperEditor(string path)
        {
            Path = path;
            TextFile = new ReaperProject(path);
        }

    }
}
