using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class EventEncoder(
    IEncoder<Background> backgroundEncoder,
    IEncoder<Video> videoEncoder,
    IEncoder<Break> breakEncoder,
    IEncoder<BackgroundColourTransformation> backgroundColourTransformationEncoder,
    IEncoder<Sprite> spriteEncoder,
    IEncoder<StoryboardSoundSample> storyboardSoundSampleEncoder,
    IEncoder<Animation> animationEncoder,
    IEncoder<ParameterCommand> parameterCommandEncoder,
    IEncoder<StandardLoop> standardLoopEncoder,
    IEncoder<TriggerLoop> triggerLoopEncoder,
    IEncoder<OtherCommand> otherCommandEncoder
) : IEncoder<Event>
{
    public EventEncoder() : this(
        new BackgroundEncoder(),
        new VideoEncoder(),
        new BreakEncoder(),
        new BackgroundColourTransformationEncoder(),
        new SpriteEncoder(),
        new StoryboardSoundSampleEncoder(),
        new AnimationEncoder(),
        new ParameterCommandEncoder(),
        new StandardLoopEncoder(),
        new TriggerLoopEncoder(),
        new OtherCommandEncoder()
    ) { }

    public string Encode(Event obj) => obj switch
    {
        Background background => backgroundEncoder.Encode(background),
        Video video => videoEncoder.Encode(video),
        Break br => breakEncoder.Encode(br),
        BackgroundColourTransformation bct => backgroundColourTransformationEncoder.Encode(bct),
        Sprite sprite => spriteEncoder.Encode(sprite),
        StoryboardSoundSample sample => storyboardSoundSampleEncoder.Encode(sample),
        Animation animation => animationEncoder.Encode(animation),
        ParameterCommand param => parameterCommandEncoder.Encode(param),
        StandardLoop stdLoop => standardLoopEncoder.Encode(stdLoop),
        TriggerLoop triggerLoop => triggerLoopEncoder.Encode(triggerLoop),
        OtherCommand other => otherCommandEncoder.Encode(other),
        _ => throw new ArgumentOutOfRangeException(nameof(obj)),
    };
}