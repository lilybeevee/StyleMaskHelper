module StyleMaskHelperHeatWaveOneMode

using ..Ahorn, Maple

@mapdef Effect "StyleMaskHelper/HeatWaveOneMode" HeatWaveOneMode(only::String="*", exclude::String="", coreMode::String="Hot", colorGrade::Bool=false)

const placements = HeatWaveOneMode

Ahorn.canFgBg(effect::HeatWaveOneMode) = true, true

Ahorn.editingOptions(effect::HeatWaveOneMode) = Dict{String, Any}(
    "coreMode" => ["Hot", "Cold", "None"]
)

end