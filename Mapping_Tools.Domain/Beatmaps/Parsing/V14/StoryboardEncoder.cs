using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

public class StoryboardEncoder : IEncoder<Storyboard> {
    public string Encode(Storyboard obj) {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("[Events]");
        builder.AppendLine("//Background and Video events");
        foreach (string s in obj.BackgroundAndVideoEvents.Select(e => e.GetLine())) builder.AppendLine(s);
        builder.AppendLine("//Break Periods");
        foreach (string s in obj.BreakPeriods.Select(b => b.GetLine())) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 0 (Background)");
        foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerBackground)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 1 (Fail)");
        foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerFail)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 2 (Pass)");
        foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerPass)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 3 (Foreground)");
        foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerForeground)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Layer 4 (Overlay)");
        foreach (string s in Event.SerializeEventTree(obj.StoryboardLayerOverlay)) builder.AppendLine(s);
        builder.AppendLine("//Storyboard Sound Samples");
        foreach (string s in obj.StoryboardSoundSamples.Select(sbss => sbss.GetLine())) builder.AppendLine(s);
        if (obj.BackgroundColourTransformations.Count > 0) {
            builder.AppendLine("//Background Colour Transformations");
            foreach (string s in obj.BackgroundColourTransformations.Select(sbss => sbss.GetLine())) builder.AppendLine(s);
        }

        builder.AppendLine();
        return builder.ToString();
    }
}