using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class EventDecoder(
    IDecoder<Background> backgroundDecoder,
    IDecoder<Video> videoDecoder,
    IDecoder<Break> breakDecoder,
    IDecoder<BackgroundColourTransformation> backgroundColourTransformationDecoder,
    IDecoder<Sprite> spriteDecoder,
    IDecoder<StoryboardSoundSample> storyboardSoundSampleDecoder,
    IDecoder<Animation> animationDecoder,
    IDecoder<ParameterCommand> parameterCommandDecoder,
    IDecoder<StandardLoop> standardLoopDecoder,
    IDecoder<TriggerLoop> triggerLoopDecoder,
    IDecoder<OtherCommand> otherCommandDecoder
) : IDecoder<Event>
{
    public EventDecoder() : this(
        new BackgroundDecoder(),
        new VideoDecoder(),
        new BreakDecoder(),
        new BackgroundColourTransformationDecoder(),
        new SpriteDecoder(),
        new StoryboardSoundSampleDecoder(),
        new AnimationDecoder(),
        new ParameterCommandDecoder(),
        new StandardLoopDecoder(),
        new TriggerLoopDecoder(),
        new OtherCommandDecoder()
    ) { }

    public Event Decode(string code)
    {
        var eventType = code.Split(',')[0].Trim();
        return eventType switch
        {
            "0" or "Background" => backgroundDecoder.Decode(code),
            "1" or "Video" => videoDecoder.Decode(code),
            "2" or "Break" => breakDecoder.Decode(code),
            "3" or "Colour" => backgroundColourTransformationDecoder.Decode(code),
            "4" or "Sprite" => spriteDecoder.Decode(code),
            "5" or "Sample" => storyboardSoundSampleDecoder.Decode(code),
            "6" or "Animation" => animationDecoder.Decode(code),
            "P" => parameterCommandDecoder.Decode(code),
            "L" => standardLoopDecoder.Decode(code),
            "T" => triggerLoopDecoder.Decode(code),
            "F" or "M" or "MX" or "MY" or "S" or "V" or "R" or "C" => otherCommandDecoder.Decode(code),
            _ => throw new BeatmapParsingException("Unknown event type.", code)
        };
    }
}