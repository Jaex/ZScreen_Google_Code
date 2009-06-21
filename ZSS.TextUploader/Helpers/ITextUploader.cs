﻿namespace ZSS.TextUploaders.Helpers
{
    interface ITextUploader
    {
        string UploadText(string text);
        string UploadTextFromClipboard();
        string UploadTextFromFile(string filePath);
        string ToErrorString();
    }
}