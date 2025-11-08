namespace Mapping_Tools.Domain.Beatmaps.Events;

public enum CommandType {
    F, // Fade
    M, // Move
    MX, // Move X
    MY, // Move Y
    S, // Scale
    V, // Vector scale
    R, // Rotate
    C, // Colour
    L, // Loop
    T, // EventType-triggered loop
    P, // Parameters
}