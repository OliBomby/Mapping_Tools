using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Models;
using Mapping_Tools.Desktop.Tools.PropertyTransformer.Models;
using Mapping_Tools.Desktop.Tools.SliderPicturator.Models;
using Mapping_Tools.Desktop.Tools.Sliderator.Models;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Models;

[TestClass]
public sealed class DesktopProjectPersistenceTests
{
    [TestMethod]
    public void Deserialize_WithLegacyHitsoundStudioProject_RestoresDesktopPresentationState()
    {
        // Arrange
        const string json = """
                            {
                              "$type": "Mapping_Tools.Viewmodels.HitsoundStudioVm, Mapping Tools",
                              "ShowResults": true,
                              "HitsoundLayers": []
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        HitsoundStudioProject project = serializer.Deserialize<HitsoundStudioProject>(json);

        // Assert
        project.ShowResults.Should().BeTrue();
    }

    [TestMethod]
    public void Deserialize_WithLegacyPropertyTransformerProject_RestoresDesktopPresentationState()
    {
        // Arrange
        const string json = """
                            {
                              "$type": "Mapping_Tools.Viewmodels.PropertyTransformerVm, Mapping Tools",
                              "SyncTimeFields": true
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        PropertyTransformerProject project = serializer.Deserialize<PropertyTransformerProject>(json);

        // Assert
        project.SyncTimeFields.Should().BeTrue();
    }

    [TestMethod]
    public void Deserialize_WithLegacySlideratorProject_RestoresDesktopPresentationState()
    {
        // Arrange
        const string json = """
                            {
                              "$type": "Mapping_Tools.Viewmodels.SlideratorVm, Mapping Tools",
                              "ShowRedAnchors": true,
                              "ShowGraphAnchors": true,
                              "ManualVelocity": true,
                              "DistanceTraveled": 128.5
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        SlideratorProject project = serializer.Deserialize<SlideratorProject>(json);

        // Assert
        project.ShowRedAnchors.Should().BeTrue();
        project.ShowGraphAnchors.Should().BeTrue();
        project.ManualVelocity.Should().BeTrue();
        project.DistanceTraveled.Should().Be(128.5);
    }

    [TestMethod]
    public void SerializeAndDeserialize_SlideratorProject_WithLoadedHitObjects_PreservesImportedState()
    {
        // Arrange
        HitObject firstSlider = new("64,64,0,2,0,L|164:64,1,100");
        HitObject secondSlider = new("164,64,1000,2,0,L|264:64,1,100");
        SlideratorProject project = new()
        {
            LoadedHitObjects = [firstSlider, secondSlider],
            VisibleHitObjectIndex = 1,
            DoEditorRead = true,
        };
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(project);
        SlideratorProject restored = serializer.Deserialize<SlideratorProject>(json);

        // Assert
        json.Should().Contain("\"LoadedHitObjects\"");
        json.Should().Contain($"\"Line\": \"{firstSlider.Line}\"");
        restored.LoadedHitObjects.Select(slider => slider.Line).Should().Equal(firstSlider.Line, secondSlider.Line);
        restored.VisibleHitObjectIndex.Should().Be(1);
        restored.DoEditorRead.Should().BeTrue();
    }

    [TestMethod]
    public void Deserialize_WithLegacySlideratorLoadedObjectTimingPoint_RestoresImportedState()
    {
        // Arrange
        const string json = """
                            {
                              "$type": "Mapping_Tools.Viewmodels.SlideratorVm, Mapping Tools",
                              "LoadedHitObjects": {
                                "$type": "System.Collections.ObjectModel.ObservableCollection`1[[Mapping_Tools.Classes.BeatmapHelper.HitObject, Mapping Tools]], System",
                                "$values": [
                                  {
                                    "$type": "Mapping_Tools.Classes.BeatmapHelper.HitObject, Mapping Tools",
                                    "Line": "64,64,1000,2,0,L|164:64,1,100",
                                    "TimingPoint": {
                                      "$type": "Mapping_Tools.Classes.BeatmapHelper.TimingPoint, Mapping Tools",
                                      "Meter": {
                                        "$type": "Mapping_Tools.Classes.ExternalFileUtil.TempoSignature, Mapping Tools",
                                        "TempoDenominator": 8,
                                        "TempoNumerator": 7,
                                        "PartialMeasure": true
                                      }
                                    }
                                  }
                                ]
                              },
                              "VisibleHitObjectIndex": 0,
                              "DoEditorRead": true
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        SlideratorProject project = serializer.Deserialize<SlideratorProject>(json);

        // Assert
        project.LoadedHitObjects.Should().ContainSingle();
        project.LoadedHitObjects[0].Line.Should().Be("64,64,1000,2,0,L|164:64,1,100");
        project.LoadedHitObjects[0].TimingPoint.Meter.TempoDenominator.Should().Be(8);
        project.LoadedHitObjects[0].TimingPoint.Meter.TempoNumerator.Should().Be(7);
        project.LoadedHitObjects[0].TimingPoint.Meter.PartialMeasure.Should().BeTrue();
        project.DoEditorRead.Should().BeTrue();
    }

    [TestMethod]
    public void Deserialize_WithLegacySliderPicturatorProject_RestoresDesktopPresentationState()
    {
        // Arrange
        const string json = """
                            {
                              "$type": "Mapping_Tools.Viewmodels.SliderPicturatorVm, Mapping Tools",
                              "SegmentCount": 42,
                              "UseMapComboColors": true,
                              "ComboColor": "0, 128, 255",
                              "TrackColorPickerColor": "#FFFFFFFF",
                              "SelectedSlider": {
                                "$type": "Mapping_Tools.Classes.BeatmapHelper.HitObject, Mapping Tools",
                                "Line": "32,64,100,2,0,L|200:64,1,168"
                              }
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        SliderPicturatorProject project = serializer.Deserialize<SliderPicturatorProject>(json);

        // Assert
        project.SegmentCount.Should().Be(42);
        project.UseMapComboColors.Should().BeTrue();
        project.ComboColor.Should().Be(new RgbaColour(255, 0, 128, 255));
        project.TrackColorPickerColor.Should().Be(new RgbaColour(255, 255, 255, 255));
        project.SetTrackColorOverride.Should().BeFalse();
        project.SelectedSlider.Should().NotBeNull();
        project.SelectedSlider!.Line.Should().Be("32,64,100,2,0,L|200:64,1,168");
    }

    [TestMethod]
    public void SerializeAndDeserialize_SliderPicturatorProject_PreservesDesktopTrackColorState()
    {
        // Arrange
        SliderPicturatorProject project = new() { UseMapComboColors = true, SegmentCount = 42 };
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(project);
        SliderPicturatorProject restored = serializer.Deserialize<SliderPicturatorProject>(json);

        // Assert
        json.Should().Contain("Mapping_Tools.Viewmodels.SliderPicturatorVm, Mapping Tools");
        restored.UseMapComboColors.Should().BeTrue();
        restored.SetTrackColorOverride.Should().BeFalse();
        restored.SegmentCount.Should().Be(42);
    }

    [TestMethod]
    public void SerializeAndDeserialize_SliderPicturatorProject_WithSelectedSlider_PreservesSliderAndOmitsBackgroundColor()
    {
        // Arrange
        HitObject selectedSlider = new("32,64,100,2,0,L|200:64,1,168");
        SliderPicturatorProject project = new()
        {
            SelectedSlider = selectedSlider,
            BackgroundColor = RgbaColour.FromArgb(77, 1, 2, 3),
        };
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(project);
        SliderPicturatorProject restored = serializer.Deserialize<SliderPicturatorProject>(json);

        // Assert
        json.Should().Contain("\"SelectedSlider\"");
        json.Should().Contain($"\"Line\": \"{selectedSlider.Line}\"");
        json.Should().NotContain("BackgroundColor");
        restored.SelectedSlider.Should().NotBeNull();
        restored.SelectedSlider!.Line.Should().Be(selectedSlider.Line);
        restored.BackgroundColor.Should().Be(RgbaColour.FromRgb(0, 0, 0));
    }
}
