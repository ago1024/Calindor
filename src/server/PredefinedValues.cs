/*
 * Copyright (C) 2007 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */

namespace Calindor.Misc.Predefines
{
    public enum PredefinedColor
    {
        Red1 = 0,
        Red2 = 7,
        Red3 = 14,
        Red4 = 21,
        Orange1 = 1,
        Orange2 = 8,
        Orange3 = 15,
        Orange4 = 22,
        Yellow1 = 2,
        Yellow2 = 9,
        Yellow3 = 16,
        Yellow4 = 23,
        Green1 = 3,
        Green2 = 10,
        Green3 = 17,
        Green4 = 24,
        Blue1 = 4,
        Blue2 = 11,
        Blue3 = 18,
        Blue4 = 25,
        Purple1 = 5,
        Purple2 = 12,
        Purple3 = 19,
        Purple4 = 26,
        Grey1 = 6,
        Grey2 = 13,
        Grey3 = 20,
        Grey4 = 27,
    }

    public enum PredefinedChannel
    {
        CHAT_LOCAL = 0,
        CHAT_PERSONAL = 1,
        CHAT_GM = 2,
        CHAT_SERVER = 3,
        CHAT_MOD = 4,
        CHAT_CHANNEL1 = 5,
        CHAT_CHANNEL2 = 6,
        CHAT_CHANNEL3 = 7,
        CHAT_MODPM = 8,
        CHAT_POPUP = 0xFF
    }

    public enum PredefinedModelHead
    {
        HEAD_1 = 0,
        HEAD_2 = 1,
        HEAD_3 = 2,
        HEAD_4 = 3,
        HEAD_5 = 4
    }

    public enum PredefinedEntityType
    {
        HUMAN_FEMALE = 0,
        HUMAN_MALE = 1,
        ELF_FEMALE = 2,
        ELF_MALE = 3,
        DWARF_FEMALE = 4,
        DWARF_MALE = 5,
        GNOME_FEMALE = 37,
        GNOME_MALE = 38,
        ORCHAN_FEMALE = 39,
        ORCHAN_MALE = 40,
        DRAEGONI_FEMALE = 41,
        DRAEGONI_MALE = 42,
    }

    public enum PredefinedModelSkin
    {
        SKIN_BROWN = 0,
        SKIN_NORMAL = 1,
        SKIN_PALE = 2,
        SKIN_TAN = 3,
        SKIN_DARK_BLUE = 4,	// for Elf
        SKIN_WHITE = 5	// for Draegoni
    }

    public enum PredefinedModelHair
    {
        HAIR_BLACK = 0,
        HAIR_BLOND = 1,
        HAIR_BROWN = 2,
        HAIR_GRAY = 3,
        HAIR_RED = 4,
        HAIR_WHITE = 5,
        HAIR_BLUE = 6,
        HAIR_GREEN = 7,
        HAIR_PURPLE = 8,
        HAIR_DARK_BROWN = 9,
        HAIR_STRAWBERRY = 10,
        HAIR_LIGHT_BLOND = 11,
        HAIR_DIRTY_BLOND = 12,
        HAIR_BROWN_GRAY = 13,
        HAIR_DARK_GRAY = 14,
        HAIR_DARK_RED = 15,
    }

    public enum PredefinedModelShirt
    {
        SHIRT_BLACK = 0,
        SHIRT_BLUE = 1,
        SHIRT_BROWN = 2,
        SHIRT_GREY = 3,
        SHIRT_GREEN = 4,
        SHIRT_LIGHTBROWN = 5,
        SHIRT_ORANGE = 6,
        SHIRT_PINK = 7,
        SHIRT_PURPLE = 8,
        SHIRT_RED = 9,
        SHIRT_WHITE = 10,
        SHIRT_YELLOW = 11,
    }

    public enum PredefinedEntityImplementationKind
    {
        ENTITY = 0,
        CLIENT_ENTITY = 1,
        SERVER_NPC = 2,
        SERVER_ENTITY = 3,
    }

    public enum PredefinedModelPants
    { 
        PANTS_BLACK = 0,
        PANTS_BLUE = 1,
        PANTS_BROWN = 2,
        PANTS_DARKBROWN = 3,
        PANTS_GREY = 4,
        PANTS_GREEN = 5,
        PANTS_LIGHTBROWN = 6,
        PANTS_RED = 7,
        PANTS_WHITE = 8,
    }

    public enum PredefinedModelBoots
    {
        BOOTS_BLACK = 0,
        BOOTS_BROWN = 1,
        BOOTS_DARKBROWN = 2,
        BOOTS_DULLBROWN = 3,
        BOOTS_LIGHTBROWN = 4,
        BOOTS_ORANGE = 5,
    }

    public enum PredefinedActorCommand
    {
        nothing = 0,
        kill_me = 1,
        die1 = 3,
        die2 = 4,
        pain1 = 5,
        pain2 = 17,
        pick = 6,
        drop = 7,
        idle = 8,
        harvest = 9,
        cast = 10,
        ranged = 11,
        meele = 12,
        sit_down = 13,
        stand_up = 14,
        turn_left = 15,
        turn_right = 16,
        enter_combat = 18,
        leave_combat = 19,

        move_n = 20,
        move_ne = 21,
        move_e = 22,
        move_se = 23,
        move_s = 24,
        move_sw = 25,
        move_w = 26,
        move_nw = 27,


        run_n = 30,
        run_ne = 31,
        run_e = 32,
        run_se = 33,
        run_s = 34,
        run_sw = 35,
        run_w = 36,
        run_nw = 37,

        turn_n = 38,
        turn_ne = 39,
        turn_e = 40,
        turn_se = 41,
        turn_s = 42,
        turn_sw = 43,
        turn_w = 44,
        turn_nw = 45,

        attack_up_1 = 46,
        attack_up_2 = 47,
        attack_up_3 = 48,
        attack_up_4 = 49,
        attack_down_1 = 50,
        attack_down_2 = 51,
    }

    public enum PredefinedActorFrame
    {
        frame_walk = 0,
        frame_run = 1,
        frame_die1 = 2,
        frame_die2 = 3,
        frame_pain1 = 4,
        frame_pain2 = 11,
        frame_pick = 5,
        frame_drop = 6,
        frame_idle = 7,
        frame_harvest = 8,
        frame_cast = 9,
        frame_ranged = 10,
        frame_sit = 12,
        frame_stand = 13,
        frame_sit_idle = 14,
        frame_combat_idle = 15,
        frame_in_combat = 16,
        frame_out_combat = 17,
        frame_attack_up_1 = 18,
        frame_attack_up_2 = 19,
        frame_attack_up_3 = 20,
        frame_attack_up_4 = 21,
        frame_attack_down_1 = 22,
        frame_attack_down_2 = 23,
    }

    public enum PredefinedDirection
    {
        N = 0,
        NE = 1,
        E = 2,
        SE = 3,
        S = 4,
        SW = 5,
        W = 6,
        NW = 7
    }
}