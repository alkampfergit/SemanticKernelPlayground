import subprocess
from semantic_kernel.skill_definition import sk_function
import os

class AudioVideo:

    @sk_function(
        description="extract audio in wav format from an mp4 file",
        name="ExtractAudio",
        input_description="Full path to the mp4 file",
    )

    def extract_audio(self, videofile: str) -> str:
        """
        Extract audio from a video file and return the full path to the extracted file.

        :param videofile: Full path to the mp4 file 
        :return: full path to the extracted audio file
        """
        # first of all change the extension to the video file to create output path
        audio_path = videofile.replace(".mp4", ".wav")
        command = f'ffmpeg -i {videofile} -vn -acodec pcm_s16le -ar 44100 -ac 2 {audio_path}'
        with open(os.devnull, 'w') as devnull:
            subprocess.call(command, shell=True, stdout=devnull, stderr=devnull)

        # now ffmpeg has created the audio file, return the path to it
        return audio_path