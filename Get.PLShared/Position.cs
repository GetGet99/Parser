﻿namespace Get.PLShared;

public record struct Position(int Line, int Char)
{
    public override string ToString() => $"{Line}:{Char}";
}