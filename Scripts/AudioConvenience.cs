public static class AudioConvenience
{
    public static Tiko.AudioSystem.AudioHandle PlaySFX(this Tiko.AudioSystem.AudioManager m, EAudio e)
        => m.PlaySFX(e.ToString());
    public static void PlayMusic(this Tiko.AudioSystem.AudioManager m, EAudio e)
        => m.PlayMusic(e.ToString());
}
