define(['loading', 'emby-input', 'emby-select', 'emby-checkbox', 'emby-button'], function (loading) {
    'use strict';

    function loadPage(page, config) {

        page.querySelector('#txtRpdbApiKey').value = config.UserApiKey || '';

        page.querySelector('#posterType').value = config.PosterType || 'poster-default';

        page.querySelector('#textless').checked = !!(config.Textless === '1')

        page.querySelector('#backdrops').checked = !!(config.Backdrops === '1')

        loading.hide();
    }

    function onSubmit(e) {

        e.preventDefault();

        loading.show();

        var form = this;

        ApiClient.getNamedConfiguration("rpdb").then(function (config) {

            config.UserApiKey = form.querySelector('#txtRpdbApiKey').value;

            config.PosterType = form.querySelector('#posterType').value;

            config.Textless = form.querySelector('#textless:checked') !== null ? '1' : '0'

            config.Backdrops = form.querySelector('#backdrops:checked') !== null ? '1' : '0'

            ApiClient.updateNamedConfiguration("rpdb", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    return function (view, params) {

        view.querySelector('form').addEventListener('submit', onSubmit);

        view.addEventListener('viewshow', function () {

            loading.show();

            var page = this;

            ApiClient.getNamedConfiguration("rpdb").then(function (response) {
                loadPage(page, response);
            });
        });
    };

});