/* -------------------------------------------
   Project Builder

   Readme:
   This script generates a C# file with the GUIDs of the FMOD project.
   If you wanna adjust file paths & names, change the lines containing "CHANGE THIS" in this script.
   -------------------------------------------
 */

studio.menu.addMenuItem({
    // CHANGE THIS FOR NEW PROJECTS
    name: "Build",
    execute: function() { main(); },
});

function main(){
    buildProject();
    buildReferences();
    alert("Save and Build complete!");
}

function buildProject() {
    studio.project.save();
    studio.project.build();
    console.log("Project Save and Build complete!");
}

function buildReferences(){
    // CHANGE THIS FOR NEW PROJECTS
    var headerFileName = "FmodGuids.cs";
    var outputPath = studio.project.filePath;
    var projectName = outputPath.substr(outputPath.lastIndexOf("/") + 1, outputPath.length);
    // CHANGE THIS FOR NEW PROJECTS (Unity project folder name)
    outputPath = outputPath.substr(0, outputPath.indexOf("FmodStudio")) + "/Assets/Scripts/" + headerFileName;

    var textFile = studio.system.getFile(outputPath);
    if (!textFile.open(studio.system.openMode.WriteOnly)) {
        alert("Failed to open file {0}\n\nCheck the file is not read-only.".format(outputPath));
        console.error("Failed to open file {0}.".format(outputPath));
        return;
    }

    textFile.writeText("/*\r\n    " + headerFileName + " - FMOD Studio API\r\n\r\n    Generated GUIDs for project '" + projectName +"'\r\n*/\r\n\r\n");
    textFile.writeText("using System;\r\n");
    textFile.writeText("using System.Collections.Generic;\r\n\r\n")
    // CHANGE THIS FOR NEW PROJECTS
    textFile.writeText("namespace Audio\r\n{\r\n");

    writeAllGUIDs(textFile);

    textFile.writeText("}\r\n\r\n");
    textFile.close();

    console.log("Header file successfully created at: " + outputPath);
}

function writeAllGUIDs(textFile, headerType) {
    var allEvents = studio.project.model.Event.findInstances();
    var allBuses = studio.project.model.MixerGroup.findInstances();
    allBuses = allBuses.concat(studio.project.model.MixerReturn.findInstances());
    allBuses = allBuses.concat(studio.project.workspace.mixer.masterBus);
    var allVCAs = studio.project.model.MixerVCA.findInstances();
    var allSnapshots = studio.project.model.Snapshot.findInstances();
    var allBanks = studio.project.model.Bank.findInstances({includeDerivedTypes: true});

    printGUID(allEvents, textFile, headerType, "AudioEvent");
    printGUID(allBuses, textFile, headerType, "AudioBus");
    printGUID(allVCAs, textFile, headerType, "AudioVCA");
    printGUID(allSnapshots, textFile, headerType, "AudioSnapshot");
    printGUID(allBanks, textFile, headerType, "AudioBank");
}

function printGUID(managedObjects, textFile, headerType, managedObjectType) {
    if (managedObjects.length === 0) {
        return;
    }

    managedObjects.sort(function(a, b) {
        var pathA = a.getPath().toUpperCase();
        var pathB = b.getPath().toUpperCase();

        if (pathA < pathB) {
            return -1;
        }
        if (pathA > pathB) {
            return 1;
        }
        return 0;
    });

    textFile.writeText("    public class " + managedObjectType + "\r\n    {\r\n");

    var whitespace = "        ";
    var guidType = "public static readonly FMOD.GUID ";
    var guidConstructor = "new FMOD.GUID ";

    managedObjects.forEach(function(object) {
        if (object.isOfExactType("Event")) {
            if (object.banks.length === 0) {
                return;
            }
        }

        var identifier = identifierForObject(object);
        identifier = identifier.replace(/(^[0-9])/g, "_$1"); // Don't allow identifier to start with a number

        var guid = guidStringToStruct(object.id, headerType);
        textFile.writeText(whitespace + guidType + identifier + " = " + guidConstructor + guid + ";\r\n");
    });

    textFile.writeText("\r\n\r\n");
    textFile.writeText(whitespace + "public static readonly Dictionary<string, FMOD.GUID> " + managedObjectType + "NameToGuid = new Dictionary<string, FMOD.GUID>()\r\n");
    textFile.writeText(whitespace + "{\r\n");
    textFile.writeText(whitespace + whitespace);

    managedObjects.forEach(function(object) {
        if (object.isOfExactType("Event")) {
            if (object.banks.length === 0) {
                return;
            }
        }

        var identifier = identifierForObject(object);
        identifier = identifier.replace(/(^[0-9])/g, "_$1"); // Don't allow identifier to start with a number

        textFile.writeText('{"' + identifier + '", ' + identifier + "}, ");
    });

    textFile.writeText("\r\n" + whitespace + "};\r\n");

    textFile.writeText("    }\r\n\r\n");
}

function identifierForObject(managedObject) {
    var path = managedObject.getPath();
    var filename = path.split("/").pop();
    if (managedObject.isOfExactType("MixerMaster")) {
        filename = "MasterBus";
    }

    return filename;
}

function toInt32(unsignedValue) {
    // signed right shift treats its operands as signed 32-bit integers
    return unsignedValue >> 0;
}

function parseInt32FromHex(hex) {
    return toInt32(parseInt(hex, 16));
}

function guidStringToStruct(guidString, headerType) {
    var guidContents = guidString.replace(/[{}-]/g, "");

    // for example {1f687138-e06c-40f5-9bac-57f84bbcedd3}
    var groupedGuid = [
        guidContents.substring(0, 8),                 // the first 8 hexadecimal digits of the GUID   - 1f687138
        guidContents.substring(8, 12),                // the first group of 4 hexadecimal digits      - e06c
        guidContents.substring(12, 16),               // the second group of 4 hexadecimal digits     - 40f5
        guidContents.substring(16, 32).match(/.{2}/g) // array of 8 bytes                             - [9b, ac, 57, f8, 4b, bc, ed, d3]
    ];

    function groupToStructure(group) {
        var formatted = [
            "Data1 = " + parseInt32FromHex(group[0]),
            "Data2 = " + parseInt32FromHex(group[2] + group[1]),
            "Data3 = " + parseInt32FromHex(group[3][3] + group[3][2] + group[3][1] + group[3][0]),
            "Data4 = " + parseInt32FromHex(group[3][7] + group[3][6] + group[3][5] + group[3][4]),
        ];
        return formatted.join(", ");
    }

    return "{ " + groupToStructure(groupedGuid) + " }";
}
