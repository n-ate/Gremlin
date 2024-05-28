(function () {
    /**
     * restores circular references put out by System.Text.Json.Serializer *
     * usage: JSON.reconstitute('{ "field":"value"}', true);
     * @param {string} jsonText
     * @param {boolean} delete$Fields
     */

    window.JSON["reconstitute"] = function (jsonText, delete$Fields) {
        let obj = JSON.parse(jsonText);
        let lookup = getReferencedData(obj, delete$Fields);
        //console.log(lookup);
        obj = applyReferencedData(obj, lookup, delete$Fields, delete$Fields);
        //console.log(obj);
        return obj;
    }

    function getReferencedData(obj, delete$id) {
        let references = [];
        for (let key in obj) {
            let value = obj[key];
            if (key === '$id') references[value] = obj;
            else if (typeof value == 'object' && value) {
                let childRefs = getReferencedData(value);
                for (var k in childRefs) references[k] = childRefs[k];
            }
        }
        if (delete$id) delete obj.$id;
        return references;
    }

    function applyReferencedData(obj, references, delete$ref, delete$values) {
        for (let key in obj) {
            let value = obj[key];
            if (key === '$ref') obj = references[value];
            else if (typeof value == 'object' && value) {
                let appliedChild = applyReferencedData(value, references, delete$ref, delete$values);
                obj[key] = appliedChild;
            }
        }
        if (delete$ref) delete obj.$id;
        if (obj.$values) {
            obj = obj.$values;
            if (!delete$values) {
                obj.$values = obj; //preserves the $values property
            }
        }
        return obj;
    }
})();