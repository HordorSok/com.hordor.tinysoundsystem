using UnityEngine;

public readonly struct PlayOptions
{
    public readonly float? VolumeMul; // multiplies SoundRef.volume
    public readonly float? VolumeAdd; // adds after mul
    public readonly float? PitchMul;  // multiplies final pitch
    public readonly float? PitchAdd;  // adds after mul

    public readonly Vector3? Position;   // play at world pos
    public readonly Transform Follow;    // follow transform (2D/3D)
    public readonly bool? SpatialOverride; // override SoundRef.spatial

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

    public static PlayOptions At(Vector3 pos) => new(position: pos);
    public static PlayOptions FollowTarget(Transform t) => new(follow: t);
}
