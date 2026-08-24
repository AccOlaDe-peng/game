using Godot;

namespace Catalyst.Presentation;

/// <summary>
/// Builds textured materials and meshes for the particle-texture VFX used
/// across the projectile, pickup, orbit, reaction and telegraph systems.
/// </summary>
public static class VfxMaterials
{
    public const string ParticleBasePath = "res://assets/art/particles/";

    public static StandardMaterial3D BuildBillboardMaterial(Texture2D texture, Color color, bool additive = true)
    {
        StandardMaterial3D material = new()
        {
            AlbedoColor = color,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
            VertexColorUseAsAlbedo = true,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = additive
                ? BaseMaterial3D.BlendModeEnum.Add
                : BaseMaterial3D.BlendModeEnum.Mix,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            DisableReceiveShadows = true,
            DisableAmbientLight = true,
            DisableFog = true
        };
        if (texture is not null)
        {
            material.AlbedoTexture = texture;
        }
        return material;
    }

    public static StandardMaterial3D BuildGroundMaterial(Texture2D texture, Color color, bool additive = true)
    {
        StandardMaterial3D material = BuildBillboardMaterial(texture, color, additive);
        material.BillboardMode = BaseMaterial3D.BillboardModeEnum.Disabled;
        return material;
    }

    public static QuadMesh BuildBillboardMesh(float size, string particleName, Color color, bool additive = true)
    {
        Texture2D? texture = LoadParticle(particleName);
        QuadMesh mesh = new() { Size = new Vector2(size, size) };
        mesh.Material = BuildBillboardMaterial(texture ?? new PlaceholderTexture2D(), color, additive);
        return mesh;
    }

    public static PlaneMesh BuildGroundMesh(float size, string particleName, Color color, bool additive = true)
    {
        Texture2D? texture = LoadParticle(particleName);
        PlaneMesh mesh = new()
        {
            Size = new Vector2(size, size),
            Orientation = PlaneMesh.OrientationEnum.Y
        };
        mesh.Material = BuildGroundMaterial(texture ?? new PlaceholderTexture2D(), color, additive);
        return mesh;
    }

    public static QuadMesh BuildVerticalBeamMesh(float width, float height, string particleName, Color color, bool additive = true)
    {
        Texture2D? texture = LoadParticle(particleName);
        QuadMesh mesh = new() { Size = new Vector2(width, height) };
        StandardMaterial3D material = BuildBillboardMaterial(texture ?? new PlaceholderTexture2D(), color, additive);
        material.BillboardMode = BaseMaterial3D.BillboardModeEnum.Disabled;
        mesh.Material = material;
        return mesh;
    }

    public static Texture2D? LoadParticle(string name)
    {
        return ResourceLoader.Load<Texture2D>($"{ParticleBasePath}{name}.png");
    }
}
