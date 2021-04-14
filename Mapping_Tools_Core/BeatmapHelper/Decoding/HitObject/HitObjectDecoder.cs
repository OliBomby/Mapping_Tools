using System;
using Mapping_Tools_Core.BeatmapHelper.Decoding.HitObject.Objects;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Objects;
using Mapping_Tools_Core.Exceptions;

namespace Mapping_Tools_Core.BeatmapHelper.Decoding.HitObject {
    public class HitObjectDecoder : IDecoder<BeatmapHelper.HitObject> {
        private readonly IDecoder<HitCircle> hitCircleDecoder;
        private readonly IDecoder<Slider> sliderDecoder;
        private readonly IDecoder<Spinner> spinnerDecoder;
        private readonly IDecoder<HoldNote> holdNoteDecoder;

        public HitObjectDecoder() : this(new HitCircleDecoder(), new SliderDecoder(), new SpinnerDecoder(), new HoldNoteDecoder()) { }

        public HitObjectDecoder(
            IDecoder<HitCircle> hitCircleDecoder,
            IDecoder<Slider> sliderDecoder,
            IDecoder<Spinner> spinnerDecoder,
            IDecoder<HoldNote> holdNoteDecoder) {
            this.hitCircleDecoder = hitCircleDecoder;
            this.sliderDecoder = sliderDecoder;
            this.spinnerDecoder = spinnerDecoder;
            this.holdNoteDecoder = holdNoteDecoder;
        }

        public void Decode(BeatmapHelper.HitObject obj, string code) {
            switch (obj) {
                case HitCircle hitCircle:
                    hitCircleDecoder.Decode(hitCircle, code);
                    break;
                case Slider slider:
                    sliderDecoder.Decode(slider, code);
                    break;
                case Spinner spinner:
                    spinnerDecoder.Decode(spinner, code);
                    break;
                case HoldNote holdNote:
                    holdNoteDecoder.Decode(holdNote, code);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(obj));
            }
        }

        public BeatmapHelper.HitObject DecodeNew(string code) {
            var values = HitObjectDecodingHelper.SplitLine(code);
            var type = HitObjectDecodingHelper.GetHitObjectType(values);
            return type switch {
                HitObjectType.Circle => hitCircleDecoder.DecodeNew(code),
                HitObjectType.Slider => sliderDecoder.DecodeNew(code),
                HitObjectType.Spinner => spinnerDecoder.DecodeNew(code),
                HitObjectType.HoldNote => holdNoteDecoder.DecodeNew(code),
                _ => throw new BeatmapParsingException("Unrecognized hit object type.", code)
            };
        }
    }
}