using RpgEngine.Pixel.Presentation;

namespace RpgEngine.Tests.Pixel;

public sealed class PresentationAssetsTests
{
    [Fact]
    public void Load_ShippedPresentation_EveryLookHasAnExistingPng()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Data");
        var loaded = PresentationLoader.Load(directory);
        Assert.True(loaded.FileFound);
        Assert.Empty(loaded.Warnings);
        var looks = loaded.Presentation;
        Assert.NotEmpty(looks.Rooms);
        Assert.NotEmpty(looks.Monsters);
        Assert.NotEmpty(looks.Npcs);
        var paths = looks.Rooms.Values.Select(room => room.BackdropImage)
            .Concat(looks.Monsters.Values.Select(monster => monster.SpriteImage))
            .Concat(looks.Npcs.Values.Select(npc => npc.PortraitImage));
        Assert.All(paths, relative =>
        {
            Assert.False(string.IsNullOrWhiteSpace(relative));
            Assert.False(Path.IsPathRooted(relative));
            Assert.Equal(".png", Path.GetExtension(relative));
            var path = Path.GetFullPath(Path.Combine(directory, relative));
            Assert.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, path);
            Assert.True(File.Exists(path), $"Missing shipped picture: {relative}");
        });
    }
}
