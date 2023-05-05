window.telerikReportViewerInterop = {
    createReportViewerWidget: function (viewerElement, reportViewerOptionsString) {
        var reportViewerOptions = JSON.parse(reportViewerOptionsString);

        this.createEventHandlerReferences(reportViewerOptions);
        this.createParameterEditorReferences(reportViewerOptions);

        $(viewerElement).telerik_ReportViewer(reportViewerOptions);
    },

    destroyReportViewerWidget: function (viewerElement) {
        // If you have issues with Blazor not clearing all the DOM generated through jQuery, you may need to do it yourself.
        // For example, widgets generating popup elements outside of the current container may remain in the DOM and
        // you may need to remove them yourself (e.g., before destroying the Kendo widgets, so you can use the references they offer).
        $(viewerElement).empty();
    },

    //#region Report Viewer Methods

    getAccessibilityKeyMap: function (viewerElement) {
        return this.getViewerWidget(viewerElement).accessibilityKeyMap();
    },

    setAccessibilityKeyMap: function (viewerElement, keyMap) {
        this.getViewerWidget(viewerElement).accessibilityKeyMap(keyMap);
    },

    setAuthenticationToken: function (viewerElement, token) {
        this.getViewerWidget(viewerElement).authenticationToken(token);
    },

    bind: function (viewerElement, eventName, handlerName) {
        var handler = this.findFunction(handlerName);
        this.getViewerWidget(viewerElement).bind(eventName, handler);
    },

    unbind: function (viewerElement, eventName, handlerName) {
        var handler = this.findFunction(handlerName);
        this.getViewerWidget(viewerElement).unbind(eventName, handler);
    },

    unbindAll: function (viewerElement, eventName) {
        this.getViewerWidget(viewerElement).unbind(eventName, handler);
    },

    clearReportSource: function (viewerElement) {
        this.getViewerWidget(viewerElement).clearReportSource();
    },

    getCurrentPage: function (viewerElement) {
        return this.getViewerWidget(viewerElement).currentPage();
    },

    getPageCount: function (viewerElement) {
        return this.getViewerWidget(viewerElement).pageCount();
    },

    getPageMode: function (viewerElement) {
        var viewerPageMode = this.getViewerWidget(viewerElement).pageMode();
        return this.parseFromPageModeValue(viewerPageMode);
    },

    setPageMode: function (viewerElement, pageMode) {
        this.getViewerWidget(viewerElement).pageMode(pageMode);
    },

    refreshReport: function (viewerElement) {
        this.getViewerWidget(viewerElement).refreshReport();
    },

    getReportSource: function (viewerElement) {
        return this.getViewerWidget(viewerElement).reportSource();
    },

    setReportSource: function (viewerElement, reportSource) {
        this.getViewerWidget(viewerElement).reportSource(reportSource);
    },

    getScale: function (viewerElement) {
        var scaleObject = this.getViewerWidget(viewerElement).scale();
        return scaleObject.scale;
    },

    setScale: function (viewerElement, scale) {
        var scaleObject = this.getViewerWidget(viewerElement).scale();
        scaleObject.scale = scale;
        this.getViewerWidget(viewerElement).scale(scaleObject);
    },

    getScaleMode: function (viewerElement) {
        var scaleObject = this.getViewerWidget(viewerElement).scale();
        return this.parseFromScaleModeValue(scaleObject.scaleMode);
    },

    setScaleMode: function (viewerElement, scaleMode) {
        var scaleObject = this.getViewerWidget(viewerElement).scale();
        scaleObject.scaleMode = scaleMode;
        this.getViewerWidget(viewerElement).scale(scaleObject);
    },

    getViewMode: function (viewerElement) {
        var viewerViewMode = this.getViewerWidget(viewerElement).viewMode();
        return this.parseFromViewModeValue(viewerViewMode);
    },

    setViewMode: function (viewerElement, viewMode) {
        this.getViewerWidget(viewerElement).viewMode(viewMode);
    },

    dispose: function (viewerElement) {
        this.getViewerWidget(viewerElement).dispose();
    },

    //#endregion

    //#region Report Viewer Commands

    executeCommand: function (viewerElement, commandName, args) {
        if (args) {
            this.getViewerWidget(viewerElement).commands[commandName].exec(args);
        } else {
            this.getViewerWidget(viewerElement).commands[commandName].exec();
        }
    },

    commandChecked: function (viewerElement, commandName) {
        return this.getViewerWidget(viewerElement).commands[commandName].checked();
    },

    commandEnabled: function (viewerElement, commandName) {
        return this.getViewerWidget(viewerElement).commands[commandName].enabled();
    },

    //#endregion

    //#region Report Viewer Utils

    createEventHandlerReferences: function (reportViewerOptions) {
        for (var key in reportViewerOptions.clientEventOptions) {
            var handlerName = reportViewerOptions.clientEventOptions[key];
            reportViewerOptions[key] = this.findFunction(handlerName);
        }

        delete reportViewerOptions.clientEventOptions;
    },

    createParameterEditorReferences: function (reportViewerOptions) {
        reportViewerOptions.parameterEditors = [];

        for (var index in reportViewerOptions.clientParameterEditors) {
            var clientParameterEditor = reportViewerOptions.clientParameterEditors[index];
            reportViewerOptions.parameterEditors.push({
                match: this.findFunction(clientParameterEditor.match),
                createEditor: this.findFunction(clientParameterEditor.createEditor)
            });
        }

        delete reportViewerOptions.clientParameterEditors;
    },

    findFunction: function (name) {
        var nameParts = name.replace(/^(window\.)/, "").split(".");
        var currentObject = window;
        while (currentObject && nameParts.length > 0) {
            currentObject = currentObject[nameParts.shift()];
        }

        return currentObject;
    },

    getViewerWidget: function (viewerElement) {
        return $(viewerElement).data("telerik_ReportViewer");
    },

    // There is no string support for enums in .net core System.Text.Json:
    // https://github.com/dotnet/runtime/issues/29894
    // See Telerik.ReportViewer.Blazor.PageMode.
    parseFromPageModeValue: function (enumString) {
        switch (enumString) {
            case "CONTINUOUS_SCROLL":
                return 0;
            case "SINGLE_PAGE":
                return 1;
            default:
                return null;
        }
    },

    // See Telerik.ReportViewer.Blazor.ScaleMode.
    parseFromScaleModeValue: function (enumString) {
        switch (enumString) {
            case "FIT_PAGE_WIDTH":
                return 0;
            case "FIT_PAGE":
                return 1;
            case "SPECIFIC":
                return 2;
            default:
                return null;
        }
    },

    // See Telerik.ReportViewer.Blazor.ViewMode.
    parseFromViewModeValue: function (enumString) {
        switch (enumString) {
            case "INTERACTIVE":
                return 0;
            case "PRINT_PREVIEW":
                return 1;

            default:
                return null;
        }
    }

    //#endregion
};