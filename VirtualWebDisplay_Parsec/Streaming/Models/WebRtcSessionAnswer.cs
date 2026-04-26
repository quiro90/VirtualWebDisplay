namespace VirtualWebDisplay.Streaming.Models;

public sealed record WebRtcSessionAnswer(string Sdp, string Type, string PeerId);
