using UnityEngine;

public readonly struct PlayOptions
{
    public readonly float? VolumeMul;
    public readonly float? VolumeAdd;
    public readonly float? PitchMul;
    public readonly float? PitchAdd;

    public readonly Vector3? Position;
    public readonly Transform Follow;
    public readonly bool? SpatialOverride;

    public PlayOptions(
        float? volumeMul = null,
        float? volumeAdd = null,
        float? pitchMul = null,
        float? pitchAdd = null,
        Vector3? position = null,
        Transform follow = null,
        bool? spatialOverride = null)
    {
        VolumeMul = volumeMul;
        VolumeAdd = volumeAdd;
        PitchMul = pitchMul;
        PitchAdd = pitchAdd;
        Position = position;
        Follow = follow;
        SpatialOverride = spatialOverride;
    }
}
