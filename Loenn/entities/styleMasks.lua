local celesteEnums = require("consts.celeste_enums")

--[[ Style Masks ]]--

local stylegroundMask = {
    name = "StyleMaskHelper/StylegroundMask",
    fillColor = {0.4, 0.8, 0.8, 0.4},
    borderColor = {0.4, 0.8, 0.8, 1.0},
    placements = {
        name = "default",
        data = {
            tag = "",
            alphaFrom = 0.0,
            alphaTo = 1.0,
            entityRenderer = false,
            behindFg = true
        }
    },
    fieldOrder = {
        "x",         "y",
        "width",     "height",
        "scrollX",   "scrollY",
        "alphaFrom", "alphaTo",
        "fade",      "customFade",
        "flag",      "tag",
    }
}

local colorGradeMask = {
    name = "StyleMaskHelper/ColorGradeMask",
    fillColor = {0.8, 0.8, 0.4, 0.4},
    borderColor = {0.8, 0.8, 0.4, 1.0},
    placements = {
        name = "default",
        data = {
            colorGradeFrom = "(current)",
            colorGradeTo = "none",
            fadeFrom = 0.0,
            fadeTo = 1.0
        }
    },
    fieldOrder = {
        "x",              "y",
        "width",          "height",
        "scrollX",        "scrollY",
        "colorGradeFrom", "colorGradeTo",
        "fadeFrom",       "fadeTo",
        "fade",           "customFade",
        "flag",
    }
}

local bloomMask = {
    name = "StyleMaskHelper/BloomMask",
    fillColor = {1.0, 1.0, 1.0, 0.4},
    borderColor = {1.0, 1.0, 1.0, 1.0},
    placements = {
        name = "default",
        data = {
            baseFrom = -1.0,
            baseTo = -1.0,
            strengthFrom = -1.0,
            strengthTo = -1.0
        }
    },
    fieldOrder = {
        "x",            "y",
        "width",        "height",
        "scrollX",      "scrollY",
        "baseFrom",     "baseTo",
        "strengthFrom", "strengthTo",
        "fade",         "customFade",
        "flag",
    }
}

local lightingMask = {
    name = "StyleMaskHelper/LightingMask",
    fillColor = {0.4, 0.4, 0.4, 0.4},
    borderColor = {0.4, 0.4, 0.4, 1.0},
    placements = {
        name = "default",
        data = {
            lightingFrom = -1.0,
            lightingTo = 0.0,
            addBase = true
        }
    },
    fieldOrder = {
        "x",            "y",
        "width",        "height",
        "scrollX",      "scrollY",
        "lightingFrom", "lightingTo",
        "fade",         "customFade",
        "flag",
    }
}

local allInOneMask = {
    name = "StyleMaskHelper/AllInOneMask",
    fillColor = {0.4, 0.4, 1.0, 0.4},
    borderColor = {0.4, 0.4, 1.0, 1.0},
    placements = {
        name = "default",
        data = {
            styleTag = "",
            styleAlphaFrom = 0.0,
            styleAlphaTo = 1.0,
            entityRenderer = false,
            styleBehindFg = true,
            colorGradeFrom = "(current)",
            colorGradeTo = "(current)",
            colorGradeFadeFrom = 0.0,
            colorGradeFadeTo = 1.0,
            bloomBaseFrom = -1.0,
            bloomBaseTo = -1.0,
            bloomStrengthFrom = -1.0,
            bloomStrengthTo = -1.0,
            lightingFrom = -1.0,
            lightingTo = -1.0,
            addBaseLight = true
        }
    },
    fieldOrder = {
        "x",                  "y",
        "width",              "height",
        "scrollX",            "scrollY",
        "bloomBaseFrom",      "bloomBaseTo",
        "bloomStrengthFrom",  "bloomStrengthTo",
        "lightingFrom",       "lightingTo",
        "colorGradeFrom",     "colorGradeTo",
        "colorGradeFadeFrom", "colorGradeFadeTo",
        "styleAlphaFrom",     "styleAlphaTo",
        "fade",               "customFade",
        "flag",               "styleTag",
    }
}

--[[ Common Variables ]]--

local commonPlacementData = {
    width = 8,
    height = 8,
    scrollX = 0.0,
    scrollY = 0.0,
    fade = "None",
    customFade = "",
    flag = "",
    notFlag = false
}

local fadeOptions = {"None", "LeftToRight", "RightToLeft", "TopToBottom", "BottomToTop", "Custom"}
local colorGradeOptions = {"(current)", "(core)", unpack(celesteEnums.color_grades)}

local commonFieldInfo = {
    fade = {
        options = fadeOptions,
        editable = false
    },
    colorGradeFrom = {
        options = colorGradeOptions,
        editable = true
    },
    colorGradeTo = {
        options = colorGradeOptions,
        editable = true
    }
}

--[[ Output ]]--

local styleMasks = {
    stylegroundMask,
    colorGradeMask,
    bloomMask,
    lightingMask,
    allInOneMask
}

for _, mask in ipairs(styleMasks) do
    -- Merge common data into each mask
    for k, v in pairs(commonPlacementData) do
        if mask.placements.data[k] == nil then
            mask.placements.data[k] = v
        end
    end

    -- Define dropdown fields
    mask.fieldInformation = commonFieldInfo

    -- Make mask render below everything
    mask.depth = math.huge
end

return styleMasks