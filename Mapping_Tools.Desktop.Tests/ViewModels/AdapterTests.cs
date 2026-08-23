using System.ComponentModel;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Core.Tools.TumourGenerating;
using Mapping_Tools.Desktop.ViewModels.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class AdapterTests
{
    [TestMethod]
    public void ObservableHitsoundZone_WhenEdited_RaisesChangesAndSnapshotsWithoutSelection()
    {
        // Arrange
        ObservableHitsoundZone adapter = new(new HitsoundZone());
        List<string?> changedProperties = [];
        adapter.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        // Act
        adapter.Name = "Whistle zone";
        adapter.XPos = 128;
        adapter.IsSelected = true;
        var snapshot = adapter.Snapshot();

        // Assert
        changedProperties.Should().Contain(nameof(ObservableHitsoundZone.Name));
        changedProperties.Should().Contain(nameof(ObservableHitsoundZone.XPos));
        changedProperties.Should().Contain(nameof(ObservableHitsoundZone.IsSelected));
        snapshot.Name.Should().Be("Whistle zone");
        snapshot.XPos.Should().Be(128);
        snapshot.Should().BeOfType<HitsoundZone>();
    }

    [TestMethod]
    public void ObservableColourPoint_WhenSequenceChanges_SnapshotsEditedSequence()
    {
        // Arrange
        SpecialColour colour = new(new RgbaColour(255, 10, 20, 30), "Combo1");
        ObservableColourPoint adapter = new(new ColourPoint(12, [], ColourPointMode.Normal));

        // Act
        adapter.Time = 24;
        adapter.Mode = ColourPointMode.Burst;
        adapter.ColourSequence.Add(new ObservableSpecialColour(colour));
        var snapshot = adapter.Snapshot();

        // Assert
        snapshot.Time.Should().Be(24);
        snapshot.Mode.Should().Be(ColourPointMode.Burst);
        snapshot.ColourSequence.Should().ContainSingle().Which.Should().BeEquivalentTo(colour);
    }

    [TestMethod]
    public void ObservableTumourLayer_WhenEdited_RaisesChangesAndCreatesPlainSnapshot()
    {
        // Arrange
        ObservableTumourLayer adapter = new(TumourLayer.GetDefaultLayer());
        PropertyChangedEventArgs? lastChange = null;
        adapter.PropertyChanged += (_, args) => lastChange = args;

        // Act
        adapter.Name = "Outer";
        adapter.TumourCount = 3;
        var snapshot = adapter.Snapshot();

        // Assert
        lastChange!.PropertyName.Should().Be(nameof(ObservableTumourLayer.TumourCount));
        snapshot.Name.Should().Be("Outer");
        snapshot.TumourCount.Should().Be(3);
        snapshot.Should().BeOfType<TumourLayer>();
    }

    [TestMethod]
    public void ObservableSampleGeneratingArgs_WhenVelocityChanges_RaisesDerivedNotificationsAndSnapshots()
    {
        // Arrange
        ObservableSampleGeneratingArgs adapter = new(new SampleGeneratingArgs("source.wav"));
        List<string?> changedProperties = [];
        adapter.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        // Act
        adapter.Velocity = 64;
        var snapshot = adapter.Snapshot();

        // Assert
        changedProperties.Should().Contain(nameof(ObservableSampleGeneratingArgs.Velocity));
        changedProperties.Should().Contain(nameof(ObservableSampleGeneratingArgs.Volume));
        snapshot.Velocity.Should().Be(64);
        snapshot.Path.Should().Be("source.wav");
    }

    [TestMethod]
    public void ObservableHitsoundLayer_WhenEdited_SnapshotsPlainLayerWithoutAdapterState()
    {
        // Arrange
        HitsoundLayer model = new("Layer", SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("source.wav"), new LayerImportArgs());
        ObservableHitsoundLayer adapter = new(model);

        // Act
        adapter.Name = "Edited layer";
        adapter.SampleArgs.Path = "edited.wav";
        adapter.Times = [12, 24];
        var snapshot = adapter.Snapshot();

        // Assert
        snapshot.Name.Should().Be("Edited layer");
        snapshot.SampleArgs.Path.Should().Be("edited.wav");
        snapshot.Times.Should().Equal(12, 24);
        snapshot.Should().BeOfType<HitsoundLayer>();
    }
}
