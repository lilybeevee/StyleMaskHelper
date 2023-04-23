local heatWaveOneMode = {}

heatWaveOneMode.name = "StyleMaskHelper/HeatWaveOneMode"
heatWaveOneMode.canBackground = true
heatWaveOneMode.canForeground = true

heatWaveOneMode.defaultData = {
    coreMode = "Hot",
    colorGrade = false
}

heatWaveOneMode.fieldInformation = {
    coreMode = {
        options = {"Hot", "Cold", "None"},
        editable = false
    }
}

return heatWaveOneMode