import React from 'react';
import './Page.css'
import * as PageNames from "../../../Constants/PageNames";
import MainPage from "../MainPage/MainPage";
import DecksPage from "../DecksPage/DecksPage";
import GamePage from "../GamePage/GamePage";
import CreatePage from "../CreatePage/CreatePage";
import ViewDeckPage from "../ViewDeckPage/ViewDeckPage";
import ErrorPage from "../ErrorPage/ErrorPage";

export default class Page extends React.Component {

    constructor(props, context) {
        super(props, context);

        this.pageGettersByName = {
            [PageNames.MAIN]: this.getMainPage,
            [PageNames.MY_DECKS]: this.getMyDecksPage,
            [PageNames.STANDARD_DECKS]: this.getStandardDecksPage,
            [PageNames.GAME]: this.getGamePage,
            [PageNames.CREATE]: this.getCreatePage,
            [PageNames.EDIT]: this.getEditPage,
            [PageNames.VIEW]: this.getViewPage,
            [PageNames.ERROR]: this.getErrorPage,
        }
    }

    render() {
        const {pageName} = this.props;

        const pageGetter = this.pageGettersByName[pageName];

        return pageGetter ? pageGetter() : <h1>Can't find page with name {pageName}</h1>
    }

    getMainPage = () => (
        <MainPage onLogin={this.props.authorize} isDarkTheme={this.props.isDarkTheme}/>
    );

    getMyDecksPage = () => (
        <DecksPage
            isUsers={true}
            onPlay={this.play}
            onView={this.edit}
            onCreate={this.create}
            onChooseStandard={this.showStandardDecks}
            key={PageNames.MY_DECKS}
        />
    );

    getStandardDecksPage = () => (
        <DecksPage
            isUsers={false}
            onView={this.view}
            onAdd={this.add}
            key={PageNames.STANDARD_DECKS}
        />
    );

    getGamePage = () => (
        <GamePage deckId={this.deckId} onEnd={this.showMyDecks} deckName={this.deckName}/>
    );

    getCreatePage = () => (
        <CreatePage onBack={this.showMyDecks}/>
    );

    getEditPage = () => (
        <ViewDeckPage
            deckId={this.deckId}
            isEditable={true}
            onBack={this.showMyDecks}
            deckName={this.deckName}
        />
    );

    getViewPage = () => (
        <ViewDeckPage
            deckId={this.deckId}
            isEditable={false}
            onBack={this.showStandardDecks}
            deckName={this.deckName}
        />
    );

    getErrorPage = () => (
        <ErrorPage/>
    );

    play = (deckId, deckName) => {
        this.deckId = deckId;
        this.deckName = deckName;
        this.props.setPageName(PageNames.GAME);
    };

    edit = (deckId, deckName) => {
        this.deckId = deckId;
        this.deckName = deckName;
        this.props.setPageName(PageNames.EDIT);
    };

    create = () => {
        this.props.setPageName(PageNames.CREATE);
    };

    showStandardDecks = () => {
        this.props.setPageName(PageNames.STANDARD_DECKS);
    };

    view = (deckId, deckName) => {
        this.deckId = deckId;
        this.deckName = deckName;
        this.props.setPageName(PageNames.VIEW);
    };

    add = () => {
        this.props.setPageName(PageNames.MY_DECKS);
    };

    showMyDecks = () => {
        this.props.setPageName(PageNames.MY_DECKS);
    }
}